using HotelSystem.Models;
using HotelSystem.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Threading.Tasks;
using Stripe.FinancialConnections;

namespace HotelSystem.Controllers
{
    [Authorize]
    public class ComplaintController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [Authorize]
        public ActionResult MyComplaints()
        {
            string userEmail = User.Identity.Name;

            var complaints = db.Complaints
                .Where(c => c.CustomerEmail == userEmail)
                .Include(c => c.ComplaintItems.Select(ci => ci.OrderedProduct.Product))
                .OrderByDescending(c => c.SubmittedAt)
                .ToList();

            return View(complaints);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult AllComplaints()
        {
            var complaints = db.Complaints
               
                .Include(c => c.ComplaintItems.Select(ci => ci.OrderedProduct.Product))
                .OrderByDescending(c => c.SubmittedAt)
                .ToList();

            return View(complaints);
        }




        [HttpGet]
        public ActionResult Submit(int orderId)
        {
            var email = User.Identity.Name;

            var order = db.CustomerOrders.FirstOrDefault(o => o.Id == orderId && o.Email == email);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders", "OrderedProducts");
            }

            var orderedItems = db.Orderedproducts
                .Include(o => o.Product)
                .Where(o => o.CustomerOrderId == order.Id)
                .ToList();

            var model = new SubmitComplaintViewModel
            {
                CustomerOrderId = orderId,
                OrderedProducts = orderedItems // ⬅️ directly pass to model
            };

            ViewBag.Order = order;

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit(SubmitComplaintViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete the form correctly.";
                return RedirectToAction("Submit", new { orderId = model.CustomerOrderId });
            }

            if (model.SelectedOrderedProductIds == null || !model.SelectedOrderedProductIds.Any())
            {
                TempData["Error"] = "Please select at least one product you're complaining about.";
                return RedirectToAction("Submit", new { orderId = model.CustomerOrderId });
            }

            var email = User.Identity.Name;

            var order = db.CustomerOrders.FirstOrDefault(o => o.Id == model.CustomerOrderId && o.Email == email);
            if (order == null)
            {
                TempData["Error"] = "Order not found or doesn't belong to you.";
                return RedirectToAction("MyOrders", "OrderedProducts");
            }

            var complaint = new Complaint
            {
                CustomerOrderId = model.CustomerOrderId,
                CustomerEmail = email,
                Description = model.Description,
                SubmittedAt = DateTime.Now
            };

            db.Complaints.Add(complaint);
            db.SaveChanges();

            var items = db.Orderedproducts
                .Include(p => p.Product)
                .Where(p => model.SelectedOrderedProductIds.Contains(p.Id)
                            && p.CustomerOrderId == model.CustomerOrderId
                            && p.CustomerOrder.Email == email)
                .ToList();

            foreach (var orderedProduct in items)
            {
                db.ComplaintItems.Add(new ComplaintItem
                {
                    ComplaintId = complaint.Id,
                    OrderedProductId = orderedProduct.Id
                });
            }

            db.SaveChanges();

            // Generate PDF summary
            byte[] pdfBytes = GenerateComplaintPDF(order, items, model.Description);

            var fileName = $"Complaint_Order_{order.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var filePath = Server.MapPath("~/assets/complaints/");
            Directory.CreateDirectory(filePath);
            var fullFilePath = Path.Combine(filePath, fileName);
            System.IO.File.WriteAllBytes(fullFilePath, pdfBytes);

            // Send email with PDF attachment
            var emailController = new EmailController();
            var subject = $"📝 Complaint Received | Order #{order.Id}";
            var body = $@"
Hello,<br/><br/>
Your complaint for Order <strong>#{order.Id}</strong> has been received.<br/>
<strong>Description:</strong><br/>{model.Description}<br/><br/>
Please find the complaint summary attached.<br/><br/>
Regards,<br/>Durban Hotel";

            await emailController.SendEmailAsync(email, subject, body, new[] { fullFilePath });

            TempData["Success"] = "Your complaint has been submitted successfully and a summary has been sent to your email.";
            return RedirectToAction("MyOrders", "OrderedProducts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReviewComplaint(int complaintId, string action, string AdminResponse)
        {
            var complaint = db.Complaints
                .Include(c => c.ComplaintItems.Select(ci => ci.OrderedProduct.Product))
                .FirstOrDefault(c => c.Id == complaintId);

            if (complaint == null)
            {
                TempData["Error"] = "Complaint not found.";
                return RedirectToAction("AllComplaints");
            }

            if (action == "Declined" && string.IsNullOrWhiteSpace(AdminResponse))
            {
                TempData["Error"] = "Please provide a reason for declining the complaint.";
                return RedirectToAction("AllComplaints");
            }

            complaint.Status = action;
            complaint.AdminResponse = AdminResponse;
            complaint.ReviewedAt = DateTime.Now;
            db.Entry(complaint).State = EntityState.Modified;

            double refundTotal = 0;

            if (action == "Approved")
            {
                var customer = db.CustInfos.FirstOrDefault(c => c.Email == complaint.CustomerEmail);
                if (customer != null)
                {
                    foreach (var item in complaint.ComplaintItems)
                    {
                        var op = item.OrderedProduct;
                        if (op != null && op.Product != null)
                        {
                            refundTotal += (double)op.Product.Price * op.Quantity;
                        }
                    }

                    customer.AccountBalance += refundTotal;
                    db.Entry(customer).State = EntityState.Modified;

                    db.WalletTransactions.Add(new WalletTransaction
                    {
                        UserEmail = customer.Email,
                        Amount = refundTotal,
                        BalanceAfterTransaction = customer.AccountBalance,
                        Type = "Credit",
                        Description = $"Refund issued for approved complaint #{complaintId}",
                        Date = DateTime.Now
                    });
                }
            }

            db.SaveChanges();

            // Prepare and send email notification
            var emailBody = $@"
        <p>Dear customer,</p>
        <p>Your complaint (#{complaintId}) regarding Order #{complaint.CustomerOrderId} has been <strong>{action}</strong>.</p>
        <p><strong>Admin Response:</strong> {AdminResponse}</p>";

            if (action == "Approved")
            {
                emailBody += $"<p>A refund of <strong>R {refundTotal:F2}</strong> has been credited to your wallet.</p>";
            }

            emailBody += "<p>Thank you for your feedback.<br>Durban Hotel Support</p>";

            var emailController = new EmailController();
            await emailController.SendEmailAsync(complaint.CustomerEmail, $"Your Complaint #{complaintId} Was {action}", emailBody);

            TempData["Success"] = $"Complaint #{complaintId} has been {action.ToLower()} and the customer has been notified.";
            return RedirectToAction("AllComplaints");
        }


        public static byte[] GenerateComplaintPDF(CustomerOrder order, List<OrderedProduct> items, string description)
        {
            using (var memoryStream = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 40f, 40f, 60f, 40f);
                PdfWriter.GetInstance(doc, memoryStream);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);

                // Title
                doc.Add(new Paragraph("Customer Complaint Summary", titleFont));
                doc.Add(new Paragraph(" "));

                // Order Info
                PdfPTable orderTable = new PdfPTable(2);
                orderTable.WidthPercentage = 100;
                orderTable.AddCell(GetCell("Order ID:", headerFont));
                orderTable.AddCell(GetCell(order.Id.ToString(), normalFont));
                orderTable.AddCell(GetCell("Customer Email:", headerFont));
                orderTable.AddCell(GetCell(order.Email, normalFont));
                orderTable.AddCell(GetCell("Date Submitted:", headerFont));
                orderTable.AddCell(GetCell(DateTime.Now.ToString("f"), normalFont));
                orderTable.AddCell(GetCell("Room/Table:", headerFont));
                orderTable.AddCell(GetCell(order.DeliveryType == "EatIn" ? $"Table {order.TableNumber}" : $"Room {order.RoomNumber}", normalFont));
                doc.Add(orderTable);

                doc.Add(new Paragraph("\nComplaint Description:", headerFont));
                doc.Add(new Paragraph(description, normalFont));
                doc.Add(new Paragraph(" "));

                // Item List
                doc.Add(new Paragraph("Items Reported:", headerFont));
                PdfPTable itemTable = new PdfPTable(3);
                itemTable.WidthPercentage = 100;
                itemTable.SetWidths(new float[] { 50, 25, 25 });

                itemTable.AddCell(GetCell("Product", headerFont, true));
                itemTable.AddCell(GetCell("Quantity", headerFont, true));
                itemTable.AddCell(GetCell("Subtotal", headerFont, true));

                foreach (var item in items)
                {
                    itemTable.AddCell(GetCell(item.Product.Name, normalFont));
                    itemTable.AddCell(GetCell(item.Quantity.ToString(), normalFont));
                    itemTable.AddCell(GetCell("R " + (item.Quantity * item.Product.Price).ToString("F2"), normalFont));
                }

                doc.Add(itemTable);
                doc.Close();

                return memoryStream.ToArray();
            }
        }

        private static PdfPCell GetCell(string text, Font font, bool isHeader = false)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 5,
                Border = isHeader ? PdfPCell.BOTTOM_BORDER : PdfPCell.NO_BORDER
            };
            return cell;
        }

    }
}
