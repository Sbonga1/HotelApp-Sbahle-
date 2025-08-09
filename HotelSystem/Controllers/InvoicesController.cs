using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using HotelSystem.Models;
using System.Threading.Tasks;

namespace HotelSystem.Controllers
{
    public class InvoicesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Invoices
        public ActionResult Index()
        {
            return View(db.Invoices.ToList());
        }
        public ActionResult MyInvoices()
        {
            string currentUser = User.Identity.Name;
            var invoices = db.Invoices.Where(x => x.CustomerEmail == currentUser);
            return View(invoices.ToList());
        }

        // GET: Invoices/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Invoice invoice = db.Invoices.Find(id);
            if (invoice == null)
            {
                return HttpNotFound();
            }
            return View(invoice);
        }

        // GET: Invoices/Create
        public ActionResult Create( string CustEmail, string Description)
        {

            var Reservation = db.Reservations.Where(x => x.Email == CustEmail & x.Status == "Checked IN").FirstOrDefault();
            string Names = Reservation.CustomerName + " " + Reservation.CustomerSurname;
            Invoice b = new Invoice()
            {
                CustomerEmail = CustEmail,
                Invoice_Date = DateTime.Now.ToShortDateString(),
                CustomerName = Names,
                Damage_Description = Description
            };
            
            
            
            return View(b);
        }

        // POST: Invoices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,InvoiceNumber,Invoice_Date,CustomerName,CustomerEmail,Damage_Description,CostBreakdown,Total_Amount_Due,Signature")] Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                var reservation = db.Reservations
                    .FirstOrDefault(x => x.Email == invoice.CustomerEmail && x.Status == "Checked IN");

                if (reservation == null)
                {
                    ModelState.AddModelError("", "Reservation not found or not in 'Checked IN' status.");
                    return View(invoice);
                }

                invoice.Room = reservation.RoomNumber;
                invoice.resevId = reservation.Id.ToString();
                invoice.status = "Awaiting Payment";

                db.Invoices.Add(invoice);
                db.SaveChanges();

                invoice.InvoiceNumber = "INV-" + DateTime.Now.Year + "-" + invoice.Id;
                db.Entry(invoice).State = EntityState.Modified;
                db.SaveChanges();

                // Generate PDF
                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A5, 36, 36, 36, 36); // 0.5 inch margins
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    document.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                    var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                    var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                    // Header
                    var header = new Paragraph("Durban Hotel", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10
                    };
                    document.Add(header);

                    var subtitle = new Paragraph("Invoice for Damages", labelFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 15
                    };
                    document.Add(subtitle);

                    // Invoice Details Table
                    var detailsTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };
                    detailsTable.SetWidths(new float[] { 40f, 60f });

                    void AddRow(string label, string value)
                    {
                        detailsTable.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 6 });
                        detailsTable.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 6 });
                    }

                    AddRow("Invoice #:", invoice.InvoiceNumber);
                    AddRow("Room Number:", invoice.Room);
                    AddRow("Reservation ID:", invoice.resevId);
                    AddRow("Customer Name:", invoice.CustomerName);
                    AddRow("Customer Email:", invoice.CustomerEmail);
                    AddRow("Date Issued:",DateTime.Parse(invoice.Invoice_Date).ToLongDateString());

                    document.Add(detailsTable);

                    // Damage Section
                    var damageTitle = new Paragraph("Damage Description", labelFont)
                    {
                        SpacingAfter = 5
                    };
                    document.Add(damageTitle);

                    var damageDesc = new Paragraph(invoice.Damage_Description ?? "-", valueFont)
                    {
                        SpacingAfter = 15
                    };
                    document.Add(damageDesc);

                    // Cost Breakdown
                    var costTitle = new Paragraph("Cost Breakdown", labelFont)
                    {
                        SpacingAfter = 5
                    };
                    document.Add(costTitle);

                    var costDetails = new Paragraph(invoice.CostBreakdown ?? "-", valueFont)
                    {
                        SpacingAfter = 10
                    };
                    document.Add(costDetails);

                    var totalDue = new Paragraph($"Total Amount Due: R{invoice.Total_Amount_Due:F2}", titleFont)
                    {
                        Alignment = Element.ALIGN_RIGHT,
                        SpacingBefore = 10,
                        SpacingAfter = 20
                    };
                    document.Add(totalDue);

                    // Signature
                    if (!string.IsNullOrEmpty(invoice.Signature))
                    {
                        var sigTitle = new Paragraph("Authorized Signature", labelFont)
                        {
                            SpacingAfter = 5
                        };
                        document.Add(sigTitle);

                        var base64 = invoice.Signature.Substring(invoice.Signature.IndexOf(",") + 1);
                        byte[] signatureBytes = Convert.FromBase64String(base64);
                        var signatureImage = iTextSharp.text.Image.GetInstance(signatureBytes);
                        signatureImage.ScaleToFit(120f, 50f);
                        document.Add(signatureImage);
                    }

                    // Footer
                    var footer = new Paragraph("Thank you for choosing Durban Hotel.", valueFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 30
                    };
                    document.Add(footer);

                    document.Close();
                    bytes = memoryStream.ToArray();
                }

                // Save to disk
                string fileName = $"INV_{invoice.CustomerEmail}_{invoice.InvoiceNumber}.pdf";
                string filePath = Server.MapPath("~/") + fileName;
                System.IO.File.WriteAllBytes(filePath, bytes);

                // Send Email using EmailController
                string subject = $"Damage Invoice | Invoice No.: {invoice.InvoiceNumber}";
                string body = $"Dear {reservation.CustomerName} {reservation.CustomerSurname},<br/><br/>Please find attached your invoice regarding damages identified during your stay.";

                var emailController = new EmailController();
                await emailController.SendEmailAsync(
                    invoice.CustomerEmail,
                    subject,
                    body + "<br/><br/>Best regards,<br/><b>Hotel Management</b>",
                    new[] { filePath }
                );

                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        // GET: Invoices/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Invoice invoice = db.Invoices.Find(id);
            if (invoice == null)
            {
                return HttpNotFound();
            }
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,InvoiceNumber,Invoice_Date,CustomerName,CustomerEmail,Damage_Description,CostBreakdown,Total_Amount_Due")] Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                db.Entry(invoice).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(invoice);
        }

        // GET: Invoices/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Invoice invoice = db.Invoices.Find(id);
            if (invoice == null)
            {
                return HttpNotFound();
            }
            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Invoice invoice = db.Invoices.Find(id);
            db.Invoices.Remove(invoice);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
