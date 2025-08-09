using HotelSystem.Models;
using System.Web.Mvc;
using System.Data.Entity;
using System.Linq;
using System.Collections.Generic;
using System;
using HotelSystem.ViewModels;
using System.IO;
using System.Web.Helpers;
using iTextSharp.text.pdf;
using iTextSharp.text;
using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Web;
using HotelSystem.Controllers;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;


public class AdminController : Controller
{
    
    private readonly ApplicationDbContext db = new ApplicationDbContext();

    [Authorize(Roles = "Admin")]
    public ActionResult WaiterList()
    {
        var waiters = db.Waiters.OrderByDescending(w => w.CreatedAt).ToList();
        return View(waiters);
    }



    public ActionResult AssignWaiter(int orderId)
    {
        var order = db.CustomerOrders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction("Index", "OrderedProducts");
        }

        var waiters = db.Waiters.ToList();

        var model = new AssignWaiterViewModel
        {
            OrderId = orderId,
            AssignmentType = order.DeliveryType, // This should be "RoomService" or "EatIn"
            Waiters = waiters
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AssignWaiter(AssignWaiterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var assignment = new WaiterAssignment
            {
                CustomerOrderId = model.OrderId,
                WaiterId = model.WaiterId,
                AssignmentType = model.AssignmentType,
                AssignedAt = DateTime.Now
            };

            db.WaiterAssignments.Add(assignment);

            var order = db.CustomerOrders.FirstOrDefault(o => o.Id == model.OrderId);
            if (order != null)
            {
                order.Status = "Preparing";
                db.Entry(order).State = EntityState.Modified;
            }

            db.SaveChanges();

            var waiter = db.Waiters.Find(model.WaiterId);
            var customer = db.CustInfos.FirstOrDefault(c => c.Email == order.Email);
            var orderItems = db.Orderedproducts.Where(o => o.CustomerOrderId == order.Id).ToList();

            string itemList = string.Join("<br/>", orderItems.Select(i => $"{i.Product.Name} x{i.Quantity}"));
            var emailController = new EmailController();

            // Send email to Waiter
            string waiterBody = model.AssignmentType == "RoomService"
                ? $@"Hello {waiter.FullName},<br/><br/>
                You’ve been assigned a new <strong>Room Service</strong> order.<br/>
                <strong>Room:</strong> {order.RoomNumber}<br/>
                <strong>Time:</strong> {order.PreferredTime}<br/><br/>
                Items:<br/>{itemList}<br/><br/>
                Regards,<br/>Durban Hotel"
                : $@"Hello {waiter.FullName},<br/><br/>
                You’ve been assigned to an <strong>Eat-In</strong> order.<br/><br/>
                Items:<br/>{itemList}<br/><br/>
                Regards,<br/>Durban Hotel";

            await emailController.SendEmailAsync(waiter.Email, "🍽️ New Order Assignment", waiterBody);

            // Send email to Customer
            string customerBody = model.AssignmentType == "RoomService"
                ? $@"Hello {customer.Name},<br/><br/>
                Your food order is being prepared and will be delivered to <strong>Room {order.RoomNumber}</strong> shortly.<br/>
                You may expect delivery by <strong>{order.PreferredTime}</strong>.<br/><br/>
                Regards,<br/>Durban Hotel"
                : $@"Hello {customer.Name},<br/><br/>
                Your Eat-In order is being prepared. A waiter has been assigned and will serve you shortly.<br/><br/>
                Please wait in the dining area.<br/><br/>
                Regards,<br/>Durban Hotel";

            await emailController.SendEmailAsync(customer.Email, "🍽️ Your Order Is Being Prepared", customerBody);

            TempData["Success"] = "Waiter and customer notified successfully.";
            return RedirectToAction("Index", "OrderedProducts");
        }

        model.Waiters = db.Waiters.ToList();
        return View(model);
    }

    [HttpGet]
    public ActionResult CreateWaiter()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateWaiter(RegisterWaiterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var db = new ApplicationDbContext();

        var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

        // Create the Identity user
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            if (!await roleManager.RoleExistsAsync("Waiter"))
            {
                await roleManager.CreateAsync(new IdentityRole("Waiter"));
            }

            await userManager.AddToRoleAsync(user.Id, "Waiter");

            string fileName = null;
            if (model.ProfileImage != null && model.ProfileImage.ContentLength > 0)
            {
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                string path = Path.Combine(Server.MapPath("~/assets/images/"), fileName);
                model.ProfileImage.SaveAs(path);
            }

            var waiter = new Waiter
            {
                FullName = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfilePicture = fileName,
                UserId = user.Id,
                CreatedAt = DateTime.Now
            };

            db.Waiters.Add(waiter);
            db.SaveChanges();

            TempData["Success"] = "Waiter created successfully.";
            return RedirectToAction("WaiterList");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error);
        }

        return View(model);
    }

    public ActionResult WalletAnalytics(string searchTerm)
    {
        // Get all transactions for chart (no matter the search)
        var allTransactions = db.WalletTransactions.ToList();

        // Calculate chart totals
        ViewBag.TotalTopUps = allTransactions
            .Where(t => t.Type == "Credit")
            .Sum(t => t.Amount);

        ViewBag.TotalSpendings = allTransactions
            .Where(t => t.Type == "Debit")
            .Sum(t => t.Amount);

        var model = new List<CustomerWalletViewModel>();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();

            var customer = db.CustInfos
                .FirstOrDefault(c => c.Name.ToLower().Contains(searchTerm) || c.Email.ToLower().Contains(searchTerm));

            if (customer != null)
            {
                var transactions = db.WalletTransactions
                    .Where(t => t.UserEmail == customer.Email)
                    .OrderByDescending(t => t.Date)
                    .Take(5)
                    .ToList();

                model.Add(new CustomerWalletViewModel
                {
                    CustomerName = customer.Name,
                    Email = customer.Email,
                    AccountBalance = customer.AccountBalance,
                    Transactions = transactions
                });
            }
        }

        ViewBag.CurrentSearch = searchTerm;

        return View(model);
    }


    public ActionResult ExportToExcel()
    {
        var data = db.WalletTransactions.ToList();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Wallet Transactions");

            worksheet.Range("A1:E1").Merge().Value = "Wallet Transactions Report";
            worksheet.Range("A1:E1").Style.Font.Bold = true;
            worksheet.Range("A1:E1").Style.Font.FontSize = 14;
            worksheet.Range("A1:E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Row(1).Height = 25;

            var headers = new[] { "User Email", "Date", "Amount", "Type", "Description" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(2, i + 1).Value = headers[i];
                var headerCell = worksheet.Cell(2, i + 1);
                headerCell.Style.Fill.BackgroundColor = XLColor.Crimson;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            for (int i = 0; i < data.Count; i++)
            {
                var tx = data[i];
                int row = i + 3;

                worksheet.Cell(row, 1).Value = tx.UserEmail;
                worksheet.Cell(row, 2).Value = tx.Date.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 3).Value = tx.Amount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "R #,##0.00"; // 💰 Format Amount
                worksheet.Cell(row, 4).Value = tx.Type;
                worksheet.Cell(row, 5).Value = tx.Description;

                for (int col = 1; col <= 5; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Position = 0;
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "WalletTransactions.xlsx");
            }
        }
    }



    public ActionResult ExportToPdf()
    {
        var data = db.WalletTransactions.ToList();

        using (var stream = new MemoryStream())
        {
            var doc = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(doc, stream).CloseStream = false;
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
            var title = new Paragraph("Wallet Transactions Report", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };
            doc.Add(title);

            var table = new PdfPTable(5)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 3, 2, 1.5f, 1.2f, 3 });

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var headerBg = new BaseColor(220, 53, 69); // Red

            string[] headers = { "User Email", "Date", "Amount", "Type", "Description" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                table.AddCell(cell);
            }

            foreach (var tx in data)
            {
                table.AddCell(new PdfPCell(new Phrase(tx.UserEmail, cellFont)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(tx.Date.ToString("yyyy-MM-dd HH:mm"), cellFont)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase("R " + tx.Amount.ToString("F2"), cellFont)) { Padding = 4, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(tx.Type, cellFont)) { Padding = 4, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase(tx.Description, cellFont)) { Padding = 4 });
            }

            doc.Add(table);
            doc.Close();

            stream.Position = 0;
            return File(stream.ToArray(), "application/pdf", "WalletTransactions.pdf");
        }
    }




    // Display list of discounts
    public ActionResult Discounts()
    {
        var discounts = db.Discounts.ToList();

        // Fetch related products and rooms for dropdown info
        var products = db.Products.ToList();
        var rooms = db.room.ToList(); // Assuming `room` is your DbSet for rooms

        // Send them to view for item lookup
        ViewBag.Products = products;
        ViewBag.Rooms = rooms;

        return View(discounts);
    }


    public ActionResult CreateDiscount()
    {
        var itemTypes = new List<SelectListItem>
    {
        new SelectListItem { Text = "Product", Value = "Product" },
        new SelectListItem { Text = "Room", Value = "Room" }
    };
        ViewBag.ApplicableItemTypes = itemTypes;

        var products = db.Products.ToList().Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Name} - R{p.Price:F2}"
        }).ToList();

        var rooms = db.room.ToList().Select(r => new SelectListItem
        {
            Value = r.ID.ToString(),
            Text = $"Room {r.Room} - {r.RoomType} - R{r.rating:F2}"
        }).ToList();


        ViewBag.Products = products;
        ViewBag.Rooms = rooms;

        return View();
    }




    [HttpPost]
   
    public ActionResult CreateDiscount(Discount discount, string[] ApplicableItemIds)
    {

        if (ModelState.IsValid)
        {
           
            discount.ApplicableItemIds = ApplicableItemIds != null ? string.Join(",", ApplicableItemIds) : string.Empty;

            discount.CreatedAt = DateTime.Now;
            discount.IsActive = true;  // Assuming you want to activate the discount upon creation

            db.Discounts.Add(discount);
            db.SaveChanges();

            return RedirectToAction("Discounts");
        }

        var itemTypes = new List<SelectListItem>
    {
        new SelectListItem { Text = "Product", Value = "Product" },
        new SelectListItem { Text = "Table Category", Value = "TableCategory" }
    };

        ViewBag.ApplicableItemTypes = new SelectList(itemTypes, "Value", "Text");

        
        ViewBag.Products = db.Products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = p.Name
        }).ToList();

        ViewBag.TableCategories = db.TableCategories.Select(tc => new SelectListItem
        {
            Value = tc.Id.ToString(),
            Text = tc.Name
        }).ToList();

       
        return View(discount);
    }


    // Edit an existing discount
    public ActionResult EditDiscount(int id)
    {
        var discount = db.Discounts.Find(id);
        if (discount == null)
        {
            return HttpNotFound();
        }
        return View(discount);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult EditDiscount(Discount discount)
    {
        if (ModelState.IsValid)
        {
            db.Entry(discount).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Discounts");
        }
        return View(discount);
    }

    // Delete a discount
    public ActionResult DeleteDiscount(int id)
    {
        var discount = db.Discounts.Find(id);
        if (discount != null)
        {
            db.Discounts.Remove(discount);
            db.SaveChanges();
        }
        return RedirectToAction("Discounts");
    }
    private void AddErrors(IdentityResult result)
{
    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error);
    }
}

}
