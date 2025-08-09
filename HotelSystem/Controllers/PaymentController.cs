    using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using HotelSystem.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Identity;
using Stripe;
using QRCoder;
using System.Drawing;
using iTextSharp.tool.xml;
using HotelSystem.ViewModels;
using HotelSystem.Helpers;

namespace HotelSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly EmailController emailController = new EmailController();

        public PaymentController()
        {
            StripeConfiguration.ApiKey = ConfigurationManager.AppSettings["StripeSecretKey"];
        }

        [Authorize]
        public async Task<ActionResult> PayFinalBalance(int bookingId)
        {
            var booking = db.EventBookings
                .Include("EventQuote")
                .FirstOrDefault(b => b.EventBookingId == bookingId);

            if (booking == null || !booking.IsPaid)
            {
                TempData["Error"] = "Invalid booking or deposit not paid.";
                return RedirectToAction("MyBookings");
            }

            var remainingAmount = booking.FinalTotalCost - booking.AmountPaid;
            if (remainingAmount <= 0)
            {
                TempData["Success"] = "✅ Your balance is already settled.";
                return RedirectToAction("Details", new { id = bookingId });
            }

            var email = User.Identity.Name;
            var user = db.CustInfos.FirstOrDefault(c => c.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("MyWallet", "Account");
            }

            // Wallet covers full
            if (user.AccountBalance >= (double)remainingAmount)
            {
                user.AccountBalance -= (double)remainingAmount;
                booking.AmountPaid += remainingAmount;
                booking.Status = "Fully Paid";
                booking.PaymentReference = "WALLET-FINAL-" + Guid.NewGuid().ToString().Substring(0, 8);

                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = email,
                    Date = DateTime.Now,
                    Amount = (double)remainingAmount,
                    BalanceAfterTransaction = user.AccountBalance,
                    Type = "Debit",
                    Description = "Final Event Payment via Wallet"
                });

                db.SaveChanges();

                TempData["Success"] = "✅ Final payment completed using your wallet.";
                return RedirectToAction("Details", new { id = bookingId });
            }
            else
            {
                var walletUsed = user.AccountBalance;
                var stripeAmount = (double)remainingAmount - walletUsed;

                if (walletUsed > 0)
                {
                    db.WalletTransactions.Add(new WalletTransaction
                    {
                        UserEmail = email,
                        Date = DateTime.Now,
                        Amount = walletUsed,
                        BalanceAfterTransaction = 0,
                        Type = "Debit",
                        Description = "Partial Wallet Payment for Final Event Balance"
                    });

                    user.AccountBalance = 0;
                    booking.AmountPaid += (decimal)walletUsed;
                    db.SaveChanges();
                }

                var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
                var sessionOptions = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "zar",
                        UnitAmount = (long)(stripeAmount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Final Balance - Event Booking"
                        }
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = domain + Url.Action("ConfirmFinalPayment", new { bookingId }),
                    CancelUrl = domain + Url.Action("Details", new { id = bookingId })
                };

                var session = await new SessionService().CreateAsync(sessionOptions);
                return Redirect(session.Url);
            }
        }
        [Authorize]
        public async Task<ActionResult> ConfirmFinalPayment(string session_id, int bookingId)
        {
            var session = await new SessionService().GetAsync(session_id);
            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Final payment failed or is incomplete.";
                return RedirectToAction("Details", new { id = bookingId });
            }

            var booking = db.EventBookings
                .Include("EventQuote")
                .FirstOrDefault(b => b.EventBookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status == "Confirmed – Fully Paid")
            {
                TempData["Success"] = "Final payment already processed.";
                return RedirectToAction("Details", new { id = bookingId });
            }

            var paymentAmount = (decimal)(session.AmountTotal / 100.0);
            booking.AmountPaid += paymentAmount;
            booking.IsPaid = true;
            booking.Status = "Confirmed – Fully Paid";
            booking.PaymentReference = "STRIPE-FINAL-" + Guid.NewGuid().ToString().Substring(0, 8);

            db.Entry(booking).State = EntityState.Modified;

            var cust = db.CustInfos.FirstOrDefault(c => c.Email == User.Identity.Name);
            if (cust != null)
            {
                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = cust.Email,
                    Date = DateTime.Now,
                    Amount = (double)paymentAmount,
                    BalanceAfterTransaction = cust.AccountBalance,
                    Type = "Stripe",
                    Description = "Final Event Payment"
                });
            }

            db.SaveChanges();

            try
            {
                var userEmail = cust?.Email ?? session.CustomerEmail;
                var userName = cust?.Name ?? "Customer";

                var foodSelections = db.EventFoodSelections
                    .Include("Product")
                    .Where(f => f.EventBookingId == bookingId)
                    .ToList();

                var totalFoodCost = foodSelections.Sum(s => s.Product.Price);
                var foodBreakdown = string.Join("<br/>", foodSelections
                    .GroupBy(f => f.Product.Name)
                    .Select(g => $"• {g.Key} × {g.Count()} = R {(g.Count() * g.First().Product.Price):N2}"));

                var foodSummary = $@"
<strong>🍽️ Food Selected by Guests:</strong><br/>
{foodBreakdown}
<br/><strong>Total Food Cost:</strong> R {totalFoodCost:N2}";

                var summaryHtml = booking.EventQuote.SummaryHtml +
                                  "<hr/>" + foodSummary +
                                  $"<br/><strong>🧾 Final Total:</strong> R {booking.FinalTotalCost:N2}";

                var pdf = GeneratePDF(
                    customerName: userName,
                    email: userEmail,
                    itemTitle: "Final Event Payment",
                    reference: booking.PaymentReference,
                    summaryHtml: summaryHtml,
                    total: booking.FinalTotalCost
                );

                var filePath = Server.MapPath("~/assets/EventFinalInvoice_" + bookingId + ".pdf");
                System.IO.File.WriteAllBytes(filePath, pdf);

                await new EmailController().SendEmailAsync(
                    userEmail,
                    "✅ Final Payment Received – Your Event is Ready",
                    "Attached is your final invoice.",
                    new[] { filePath });

                System.IO.File.Delete(filePath);
            }
            catch
            {
                TempData["Warning"] = "Payment saved, but email invoice failed.";
            }

            TempData["Success"] = "🎉 Final payment confirmed and event fully paid.";
            return RedirectToAction("Details", new { id = bookingId });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> PayForTicket()
        {
            var model = TempData["TicketPurchase"] as BuyTicketViewModel;
            var buyerEmail = TempData["BuyerEmail"] as string;
            var buyerName = TempData["BuyerName"] as string;

            if (model == null || string.IsNullOrEmpty(buyerEmail))
            {
                TempData["Error"] = "Ticket purchase session expired.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var ticketType = db.TicketTypes.FirstOrDefault(t => t.TicketTypeId == model.SelectedTicketTypeId);
            if (ticketType == null)
            {
                TempData["Error"] = "Invalid ticket type.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var total = ticketType.Price * model.Quantity;
            var domain = Request.Url.Scheme + "://" + Request.Url.Authority;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = buyerEmail,
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "zar",
                    UnitAmount = (long)(ticketType.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{ticketType.Name} Ticket"
                    }
                },
                Quantity = model.Quantity
            }
        },
                Mode = "payment",
                SuccessUrl = domain + "/Payment/SuccessTicketPayment?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Payment/Cancel"
            };

            var session = await new SessionService().CreateAsync(options);

            TempData["TicketPurchase"] = model;
            TempData["TicketSessionId"] = session.Id;
            TempData["BuyerEmail"] = buyerEmail;
            TempData["BuyerName"] = buyerName;

            return Redirect(session.Url);
        }

        [AllowAnonymous]
        public async Task<ActionResult> SuccessTicketPayment(string session_id)
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(session_id);

            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Payment was not successful.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var model = TempData["TicketPurchase"] as BuyTicketViewModel;
            var buyerEmail = TempData["BuyerEmail"] as string;
            var buyerName = TempData["BuyerName"] as string;

            if (model == null || string.IsNullOrEmpty(buyerEmail))
            {
                TempData["Error"] = "Ticket session lost.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var ticketType = db.TicketTypes.FirstOrDefault(t => t.TicketTypeId == model.SelectedTicketTypeId);
            if (ticketType == null)
            {
                TempData["Error"] = "Ticket type not found.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var totalPaid = ticketType.Price * model.Quantity;
            var attachmentPaths = new List<string>();
            var imagePaths = new List<string>();

            for (int i = 0; i < model.Quantity; i++)
            {
                var code = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

                // Generate QR Code
                QRCodeGenerator qrGen = new QRCodeGenerator();
                QRCodeData qrData = qrGen.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
                QRCode qr = new QRCode(qrData);
                Bitmap qrImage = qr.GetGraphic(20);

                string qrFile = $"qr_{code}.png";
                string qrPath = Server.MapPath("~/assets/images/" + qrFile);
                Directory.CreateDirectory(Server.MapPath("~/assets/images/"));
                qrImage.Save(qrPath);
                imagePaths.Add(qrPath);

                // Save ticket
                var ticket = new Ticket
                {
                    EventId = model.EventBookingId,
                    UserEmail = buyerEmail,
                    TicketType = ticketType.Name,
                    TicketCode = code,
                    Quantity = 1,
                    AmountPaid = ticketType.Price,
                    StripeReference = session.Id,
                    QRCodeImagePath = "/assets/images/" + qrFile,
                    IsPaid = true,
                    PurchaseDate = DateTime.Now,
                    IssuedAt = DateTime.Now,
                    EventBookingId = model.EventBookingId,
                };

                db.Tickets.Add(ticket);
                db.SaveChanges();

                // Generate PDF with embedded QR code
                var pdf = PdfHelper.GenerateTicketPDF(
                    customerName: buyerName,
                    email: buyerEmail,
                    ticketType: ticketType.Name,
                    quantity: 1,
                    amount: ticketType.Price, 
                    code: code,
                    eventTitle:  ticketType.Event.EventQuote.Title,
                    eventDate: ticketType.Event.EventQuote.EventStartDateTime,
                    venue: model.Venue,
                    qrImagePath: qrPath 
                );

                var pdfPath = Server.MapPath($"~/assets/Ticket_{code}.pdf");
                System.IO.File.WriteAllBytes(pdfPath, pdf);
                attachmentPaths.Add(pdfPath);
            }

            string subject = $"🎟️ Your Tickets for {model.EventTitle}";
            string body = $@"
<p>Hello {buyerName},</p>
<p>Thank you for purchasing <b>{model.Quantity}</b> ticket(s) to <strong>{model.EventTitle}</strong>.</p>
<p>Your ticket(s) are attached as PDF. Please show the QR code at the entrance.</p>
<p><b>Total Paid:</b> R {totalPaid}</p>
<hr />
<p>Warm regards,<br/>Durban Hotel Events Team</p>";
            try
            {
                await new EmailController().SendEmailAsync(
                    buyerEmail,
                    subject,
                    body,
                    attachmentPaths.ToArray()
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Failed to send ticket via email: " + ex.Message;
            }


            TempData["Success"] = "✅ Your tickets have been emailed successfully.";
            return RedirectToAction("Browse", "PublicEvent");
        }

        [Authorize]
        public async Task<ActionResult> MakePayment(int bookingId)
        {
            var booking = db.EventBookings.Include("EventQuote").FirstOrDefault(b => b.EventBookingId == bookingId);
            if (booking == null || booking.IsPaid)
            {
                TempData["Error"] = "Invalid or already paid booking.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            var email = User.Identity.Name;
            var cust = db.CustInfos.FirstOrDefault(c => c.Email == email);
            if (cust == null)
            {
                TempData["Error"] = "Customer info not found.";
                return RedirectToAction("MyWallet", "Account");
            }

            decimal depositAmount = booking.DepositRequired;
            double remaining = (double)(depositAmount - booking.AmountPaid);

            // FULL WALLET COVERAGE
            if (cust.AccountBalance >= remaining)
            {
                cust.AccountBalance -= remaining;
                booking.IsPaid = true;
                booking.AmountPaid += (decimal)remaining;
                booking.Status = "Deposit Paid";
                booking.PaymentReference = "WALLET-" + Guid.NewGuid().ToString("N").Substring(0, 8);

                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = email,
                    Date = DateTime.Now,
                    Amount = remaining,
                    BalanceAfterTransaction = cust.AccountBalance,
                    Type = "Debit",
                    Description = "50% Event Deposit via Wallet"
                });

                db.Entry(cust).State = EntityState.Modified;
                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges();

                // Email PDF receipt
                try
                {
                    var pdf = GeneratePDF(
                        customerName: cust.Name,
                        email: email,
                        itemTitle: "Event Deposit",
                        reference: booking.PaymentReference,
                        summaryHtml: booking.EventQuote.SummaryHtml,
                        total: booking.DepositRequired
                    );

                    var fileName = $"DepositReceipt_{bookingId}.pdf";
                    var filePath = Server.MapPath("~/assets/" + fileName);
                    System.IO.File.WriteAllBytes(filePath, pdf);

                    await emailController.SendEmailAsync(email,
                        $"Event Deposit Confirmation #{bookingId}",
                        "Your 50% deposit has been received. Please find your receipt attached.",
                        new[] { filePath });

                    System.IO.File.Delete(filePath);
                }
                catch
                {
                    TempData["Error"] = "Payment complete, but receipt email failed.";
                }

                TempData["Success"] = "✅ 50% deposit paid successfully using wallet.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            // PARTIAL WALLET + STRIPE
            double walletUsed = cust.AccountBalance;
            if (walletUsed > 0)
            {
                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = email,
                    Date = DateTime.Now,
                    Amount = walletUsed,
                    BalanceAfterTransaction = 0,
                    Type = "Debit",
                    Description = "Partial Wallet for 50% Deposit"
                });

                cust.AccountBalance = 0;
                db.Entry(cust).State = EntityState.Modified;
            }

            booking.AmountPaid += (decimal)walletUsed;
            db.Entry(booking).State = EntityState.Modified;
            db.SaveChanges();

            var domain = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            var stripeAmount = (long)((remaining - walletUsed) * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = email,
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "zar",
                    UnitAmount = stripeAmount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "50% Deposit - Event Booking"
                    }
                },
                Quantity = 1
            }
        },
                Mode = "payment",
                SuccessUrl = domain + $"/Payment/SuccessEvent?session_id={{CHECKOUT_SESSION_ID}}&bookingId={bookingId}",
                CancelUrl = domain + "/Payment/CancelPayment"
            };

            var session = await new SessionService().CreateAsync(options);
            return Redirect(session.Url);
        }
        [Authorize]
        public async Task<ActionResult> SuccessEvent(string session_id, int bookingId)
        {
            var session = await new SessionService().GetAsync(session_id);
            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Payment was not successful.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            var booking = db.EventBookings.Include("EventQuote").FirstOrDefault(b => b.EventBookingId == bookingId);
            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            if (booking.IsPaid)
            {
                TempData["Success"] = "Deposit already processed.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            double stripePaid = (double)(session.AmountTotal / 100.0);

            booking.AmountPaid += (decimal)stripePaid;

            if (booking.AmountPaid >= booking.DepositRequired)
            {
                booking.IsPaid = true;
                booking.Status = "Deposit Paid";
                booking.PaymentReference = "STRIPE-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            db.Entry(booking).State = EntityState.Modified;

            var cust = db.CustInfos.FirstOrDefault(x => x.Email == User.Identity.Name);
            if (cust != null)
            {
                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = cust.Email,
                    Date = DateTime.Now,
                    Amount = stripePaid,
                    BalanceAfterTransaction = cust.AccountBalance,
                    Type = "Stripe",
                    Description = "Stripe - 50% Event Deposit"
                });
            }

            db.SaveChanges();

            try
            {
                var pdf = GeneratePDF(
                    customerName: cust?.Name ?? "Customer",
                    email: cust?.Email ?? session.CustomerEmail,
                    itemTitle: "Event Deposit",
                    reference: booking.PaymentReference,
                    summaryHtml: booking.EventQuote.SummaryHtml,
                    total: booking.DepositRequired
                );

                var fileName = $"DepositReceipt_{bookingId}.pdf";
                var filePath = Server.MapPath("~/assets/" + fileName);
                System.IO.File.WriteAllBytes(filePath, pdf);

                await emailController.SendEmailAsync(session.CustomerEmail,
                    "50% Event Deposit Received",
                    "Thank you! Please find your deposit receipt attached.",
                    new[] { filePath });
            }
            catch
            {
                TempData["Error"] = "Payment complete, but email receipt failed.";
            }

            TempData["Success"] = "✅ Your 50% deposit was successfully processed.";
            return RedirectToAction("MyBookings", "EventBooking");
        }

        public async Task<ActionResult> OnceOff(double totalPrice, string email, string payment = "")
        {
            var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            string userEmail = User.Identity.GetUserName();
            var guest = db.Users.FirstOrDefault(u => u.Email == userEmail || u.Email == email);
            var custInfo = db.CustInfos.FirstOrDefault(c => c.Email == guest.Email);

            if (guest == null || custInfo == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            var reservation = db.Reservations.FirstOrDefault(r => r.Email == guest.Email && r.Status == "Received, Pending Payment");
            if (reservation == null)
            {
                TempData["Error"] = "Reservation not found.";
                return RedirectToAction("Index", "Home");
            }

            if (custInfo.AccountBalance >= totalPrice)
            {
                // Full wallet payment
                custInfo.AccountBalance -= totalPrice;

                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = guest.Email,
                    Date = DateTime.Now,
                    Amount = totalPrice,
                    BalanceAfterTransaction = custInfo.AccountBalance,
                    Type = "Debit",
                    Description = $"{payment} Payment via Wallet"
                });

                reservation.Status = "Fee Settled";
                reservation.WalletAmt = totalPrice;
                reservation.AmtPaid = 0;
                db.Entry(custInfo).State = EntityState.Modified;
                db.Entry(reservation).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Success", new
                {
                    session_id = "WALLET",
                    email = guest.Email,
                    payment = payment,
                    amount = totalPrice
                });
            }
            else
            {
                double walletUsed = custInfo.AccountBalance;
                double remainingToPay = totalPrice - walletUsed;

                if (walletUsed > 0)
                {
                    db.WalletTransactions.Add(new WalletTransaction
                    {
                        UserEmail = guest.Email,
                        Date = DateTime.Now,
                        Amount = walletUsed,
                        BalanceAfterTransaction = custInfo.AccountBalance,
                        Type = "Debit",
                        Description = $"{payment} Partial Payment via Wallet"
                    });

                    reservation.WalletAmt = walletUsed;
                    custInfo.AccountBalance = 0;
                    db.Entry(custInfo).State = EntityState.Modified;
                }

                reservation.AmtPaid = remainingToPay;
                db.Entry(reservation).State = EntityState.Modified;
                db.SaveChanges();

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    CustomerEmail = guest.Email,
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "zar",
                        UnitAmount = (long)(remainingToPay * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{payment} Remaining Balance"
                        }
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = domain + $"/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}&email={guest.Email}&payment={payment}&amount={remainingToPay}",
                    CancelUrl = domain + "/Payment/Cancel"
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Redirect(session.Url);
            }
        }

        public async Task<ActionResult> Success(string session_id, string email, string payment, double amount)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus != "paid")
            {
                ViewBag.Message = "Payment was not successful.";
                return View();
            }

            var guest = db.Users.FirstOrDefault(u => u.Email == email);
            if (guest == null)
            {
                ViewBag.Message = "Guest not found.";
                return View();
            }

            string subject = "", body = "", itemName = "Hotel Payment";
            string uniqueCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(); // Unique check-in code

            if (payment == "Reservation")
            {
                var reservation = db.Reservations.FirstOrDefault(r => r.Email == email && r.Status == "Received, Pending Payment");
                if (reservation != null)
                {
                    var custInfo = db.CustInfos.FirstOrDefault(x => x.Email == User.Identity.Name);
                    custInfo.ReservID = reservation.Id;

                    reservation.Status = "Fee Settled";
                    reservation.AmtPaid = amount;
                    reservation.CheckInCode = uniqueCode; // Store unique check-in code

                    db.Entry(custInfo).State = EntityState.Modified;
                    db.Entry(reservation).State = EntityState.Modified;
                    db.SaveChanges();

                    subject = $"Booking Statement | Ref No.: {reservation.Id}";
                    body = $"Hello {reservation.CustomerName} {reservation.CustomerSurname},<br/><br/>" +
                           $"Your payment was successful. Your check-in code is <b>{uniqueCode}</b>.<br/>" +
                           $"Present this code at the front desk or scan the attached QR code.<br/>";
                    itemName = "Reservation Payment";
                }
            }
            else if (payment == "Invoice")
            {
                var invoice = db.Invoices.FirstOrDefault(i => i.CustomerEmail == email && i.status == "Awaiting Payment");
                if (invoice != null)
                {
                    invoice.status = "Settled";
                    db.Entry(invoice).State = EntityState.Modified;
                    db.SaveChanges();

                    subject = $"Invoice Payment | INV No.: {invoice.InvoiceNumber}";
                    body = $"Hello {guest.Name},<br/><br/>Please find your invoice receipt attached.<br/>";
                    itemName = "Invoice Payment";
                }
            }

            // 📄 Generate PDF Receipt
            var pdfBytes = GeneratePDF(guest.Name, guest.Email, itemName);
            string pdfFileName = $"{itemName.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string pdfFolder = Server.MapPath("~/assets/");
            Directory.CreateDirectory(pdfFolder);
            string pdfPath = Path.Combine(pdfFolder, pdfFileName);
            System.IO.File.WriteAllBytes(pdfPath, pdfBytes);

            // 📷 Generate QR Code
            string qrFileName = $"QRCode_{uniqueCode}.png";
            string qrPath = Path.Combine(pdfFolder, qrFileName);
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(uniqueCode, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(20);
                System.IO.File.WriteAllBytes(qrPath, qrBytes);
            }

            // 📧 Send Email
            var emailController = new EmailController();
            await emailController.SendEmailAsync(
                guest.Email,
                subject,
                body + "<br/>Thanks,<br/><b>Durban Hotel</b>",
                new[] { pdfPath, qrPath } // Attach both receipt and QR code
            );

            if (payment == "Refund")
            {
                TempData["Success"] = "Refund approved Successfully, Payment made to customer.";
                return RedirectToAction("Index", "Refunds");
            }

            ViewBag.Message = "Payment successful. A QR code and check-in code have been sent to your email.";
            return View();
        }

        public ActionResult Cancel()
        {
            ViewBag.Message = "Payment was canceled.";
            return View();
        }

        public async Task<ActionResult> CreatePayment(double total, int? tableId)
        {
            CookieHelper.SetCookie("TableID", tableId.HasValue && tableId > 0 ? tableId.Value : 0, 5);

            var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            var email = User.Identity.Name;
            var custInfo = db.CustInfos.FirstOrDefault(x => x.Email == email);

            if (custInfo == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index", "Home");
            }

            double walletUsed = TempData["WalletUsed"] != null ? Convert.ToDouble(TempData["WalletUsed"]) : 0;

            if (walletUsed > 0)
            {
                custInfo.AccountBalance -= walletUsed;
                custInfo.Delivery = TempData["DeliveryOption"].ToString();
                db.WalletTransactions.Add(new WalletTransaction
                {
                    UserEmail = email,
                    Date = DateTime.Now,
                    Amount = walletUsed,
                    BalanceAfterTransaction = custInfo.AccountBalance,
                    Type = "Debit",
                    Description = "Partial Wallet Usage for Hotel Services"
                });

                db.Entry(custInfo).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            if (total <= 0)
            {
                return RedirectToAction("PaymentSuccess", new { session_id = "WALLET" });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = email,
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "zar",
                    UnitAmount = (long)(total * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Payment for Hotel Services"
                    }
                },
                Quantity = 1
            }
        },
                Mode = "payment",
                SuccessUrl = domain + "/Payment/PaymentSuccess?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Payment/CancelPayment"
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        public async Task<ActionResult> PaymentSuccess(string session_id)
        {
            string email;

            try
            {
                if (session_id == "WALLET")
                {
                    email = User.Identity.Name;
                }
                else
                {
                    var sessionService = new SessionService();
                    var session = await sessionService.GetAsync(session_id);

                    if (session == null || session.PaymentStatus != "paid")
                    {
                        TempData["Error"] = "Payment not successful.";
                        return RedirectToAction("Index", "Home");
                    }

                    email = session.CustomerEmail;
                }

                int? tableId = CookieHelper.GetAndDeleteCookie("TableID");
                if (tableId == null)
                {
                    TempData["Error"] = "No table information found.";
                    return RedirectToAction("Index", "Home");
                }

                var user = db.CustInfos.FirstOrDefault(x => x.Email == email);
                if (user == null)
                {
                    TempData["Error"] = "Customer information not found.";
                    return RedirectToAction("Index", "Home");
                }

                if (tableId == 0)
                {
                    var resrv = db.Reservations.Find(user.ReservID);
                    if (resrv == null)
                    {
                        TempData["Error"] = "Reservation not found.";
                        return RedirectToAction("Index", "Home");
                    }

                    var cart = ShoppingCart.GetCart(this.HttpContext);
                    if (cart == null)
                    {
                        TempData["Error"] = "Cart not found.";
                        return RedirectToAction("Index", "Home");
                    }

                    int code;
                    do
                    {
                        code = Meths.GenerateRandomCode();
                    } while (db.CustomerOrders.Any(r => r.UniqueCode == code));

                    QRCodeGenerator qrGen = new QRCodeGenerator();
                    QRCodeData qrData = qrGen.CreateQrCode(code.ToString(), QRCodeGenerator.ECCLevel.Q);
                    QRCode qr = new QRCode(qrData);
                    Bitmap qrImage = qr.GetGraphic(20);

                    string qrFile = $"{Guid.NewGuid()}.png";
                    string qrPath = Server.MapPath("~/assets/images/" + qrFile);
                    Directory.CreateDirectory(Server.MapPath("~/assets/images/")); // Ensure folder exists
                    qrImage.Save(qrPath);

                    var order = new CustomerOrder
                    {
                        Status = "Placed",
                        Email = email,
                        Amount = cart.GetTotal(),
                        CustomerUserName = email,
                        DateCreated = DateTime.Now,
                        LastName = user.Name,
                        DeliveryType = user.Delivery,
                        RoomNumber = int.TryParse(resrv.RoomNumber, out var room) ? room : 0,
                        UniqueCode = code,
                        qrCodePicture = qrFile,
                        PreferredTime = user.Time.ToShortTimeString()
                    };

                    db.CustomerOrders.Add(order);
                    db.SaveChanges();

                    cart.CreateOrder(order);

                    var items = db.Orderedproducts.Where(x => x.CustomerOrderId == order.Id).ToList();
                    foreach (var item in items)
                    {
                        if (item?.Product != null && item.Product.qty < 5)
                        {
                            item.Product.status = "Needs Restock";
                            db.Entry(item.Product).State = EntityState.Modified;
                        }
                    }
                    db.SaveChanges();

                    string subject = $"Payment Confirmation | Order #{order.Id}";
                    string body = $@"
Hello {order.LastName},<br/><br/>
Thank you for your payment.<br/>
Your room service order <strong>#{order.Id}</strong> has been placed.<br/><br/>
<img src='cid:QrCodeImage' alt='QR Code' /><br/>
<strong>Your Unique Code:</strong> {order.UniqueCode}<br/><br/>";

                    if (session_id == "WALLET")
                        body += "💼 Payment was completed using your hotel wallet balance.<br/><br/>";

                    body += "Regards,<br/>Durban Hotel";

                    var pdfBytes = GeneratePDF(user.Name, email, "Room Service Order");
                    string filePath = Server.MapPath("~/assets/");
                    Directory.CreateDirectory(filePath);
                    string fileName = Path.Combine(filePath, $"Order_{order.Id}.pdf");
                    System.IO.File.WriteAllBytes(fileName, pdfBytes);

                    await emailController.SendEmailWithInlineImageAsync(
                        email, subject, body, new[] { fileName }, qrPath, "QrCodeImage");

                    TempData["Success"] = session_id == "WALLET"
                        ? "Room service order placed successfully using your wallet balance."
                        : "Room service order placed successfully.";

                    return RedirectToAction("MyOrders", "OrderedProducts");
                }
                else
                {
                    var table = db.TableCategories.FirstOrDefault(t => t.Id == tableId.Value);
                    if (table == null)
                    {
                        TempData["Error"] = "Table not found.";
                        return RedirectToAction("Index", "Reservations");
                    }

                    int code;
                    do
                    {
                        code = Meths.GenerateRandomCode();
                    } while (db.TableReservations.Any(r => r.UniqueCode == code));

                    QRCodeGenerator qrGen = new QRCodeGenerator();
                    QRCodeData qrData = qrGen.CreateQrCode(code.ToString(), QRCodeGenerator.ECCLevel.Q);
                    QRCode qr = new QRCode(qrData);
                    Bitmap qrImage = qr.GetGraphic(20);

                    string qrFile = $"{Guid.NewGuid()}.png";
                    string qrPath = Server.MapPath("~/assets/images/" + qrFile);
                    Directory.CreateDirectory(Server.MapPath("~/assets/images/"));
                    qrImage.Save(qrPath);

                    var reservation = new TableReservation
                    {
                        qrCodePicture = qrFile,
                        UniqueCode = code,
                        CustomerName = user.Name,
                        ReservationDate = user.Date,
                        ReservationTime = user.Time,
                        Email = email,
                        Status = "Reserved",
                        TableCategoryId = table.Id,
                        NumberOfGuests = table.MaxGuests
                    };

                    table.Status = "Reserved";

                    db.TableReservations.Add(reservation);
                    db.Entry(table).State = EntityState.Modified;
                    db.SaveChanges();

                    string subject = $"Table Reservation Confirmed | Ref No. {reservation.Id}";
                    string body = $@"
Hello {user.Name},<br/><br/>
Your reservation has been confirmed. Please find your QR code and details attached.<br/>";

                    if (session_id == "WALLET")
                        body += "<br/>💼 Payment was completed using your hotel wallet balance.";

                    body += "<br/><br/>Regards,<br/>Durban Hotel";

                    var pdfBytes = GeneratePDF(user.Name, email, "Table Reservation");
                    string filePath = Server.MapPath("~/assets/");
                    Directory.CreateDirectory(filePath);
                    string fileName = Path.Combine(filePath, $"Reservation_{reservation.Id}.pdf");
                    System.IO.File.WriteAllBytes(fileName, pdfBytes);
                    
                    await emailController.SendEmailAsync(email, subject, body, new[] { fileName });

                    TempData["Success"] = session_id == "WALLET"
                        ? "Reservation created successfully using your wallet balance."
                        : "Reservation created successfully.";

                    return RedirectToAction("Index", "Reservations");
                }
            }
            catch (Exception ex)
            {
                string errorPath = Server.MapPath("~/assets/error_log.txt");
                System.IO.File.WriteAllText(errorPath, DateTime.Now + " - " + ex.ToString());
                TempData["Error"] = "An unexpected error occurred. Please try again or contact support. " +ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult CancelPayment()
        {
            TempData["Error"] = "Payment was cancelled.";
            return RedirectToAction("Index", "Home");
        }
        private byte[] GeneratePDF(string customerName, string email, string itemTitle, string reference, string summaryHtml, decimal total)
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A5, 36, 36, 36, 36);
                var writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                document.Add(new Paragraph("Durban Hotel", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5
                });

                document.Add(new Paragraph(itemTitle, labelFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                });

                var table = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10,
                    SpacingAfter = 20
                };
                table.SetWidths(new float[] { 30f, 70f });

                void AddRow(string label, string value)
                {
                    table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 5 });
                    table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 5 });
                }

                AddRow("Customer:", customerName);
                AddRow("Email:", email);
                AddRow("Date:", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                AddRow("Reference:", reference);
                AddRow("Payment For:", itemTitle);
                AddRow("Total:", "R " + total.ToString());

                document.Add(table);

                document.Add(new Paragraph("Cost Breakdown:", labelFont) { SpacingBefore = 10 });

                using (var htmlMs = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(summaryHtml)))
                using (var htmlReader = new StreamReader(htmlMs))
                {
                    XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, htmlReader);
                }

                document.Add(new Paragraph("\nThank you for your payment!", valueFont) { SpacingBefore = 20 });
                document.Close();

                return memoryStream.ToArray();
            }
        }
        private byte[] GeneratePDF(string customerName, string email, string itemTitle)
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A5, 36, 36, 36, 36);
                PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                document.Add(new Paragraph("Durban Hotel", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5
                });

                document.Add(new Paragraph(itemTitle, labelFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                });

                var table = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10,
                    SpacingAfter = 20
                };
                table.SetWidths(new float[] { 30f, 70f });

                void AddRow(string label, string value)
                {
                    table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 5 });
                    table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 5 });
                }

                AddRow("Customer:", customerName);
                AddRow("Email:", email);
                AddRow("Date:", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                AddRow("Payment For:", itemTitle);

                document.Add(table);
                document.Add(new Paragraph("Thank you for your payment!", valueFont) { SpacingBefore = 20 });
                document.Close();

                return memoryStream.ToArray();
            }
        }

    }
}

