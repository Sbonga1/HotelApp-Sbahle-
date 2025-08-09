using HotelSystem.Helpers;
using HotelSystem.Models;
using HotelSystem.ViewModels;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using iTextSharp.tool.xml;

namespace HotelSystem.Controllers
{
    [Authorize]
    public class EventBookingController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ✅ Admin: View All Events
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var allBookings = db.EventBookings
                .Include("EventQuote.Venue")
                .OrderByDescending(b => b.ConfirmedAt)
                .ToList();

            return View(allBookings);
        }

        public ActionResult MyBookings()
        {
            string userEmail = User.Identity.Name;
            var userBookings = db.EventBookings
                .Include("EventQuote.Venue")
                .Where(b => b.UserEmail == userEmail)
                .OrderByDescending(b => b.ConfirmedAt)
                .ToList();

            return View(userBookings);
        }

        public ActionResult Details(int id)
        {
            var booking = db.EventBookings
                .Include("EventQuote")
                .Include("EventQuote.Venue")
                .Include("EventQuote.SelectedActivities")
                .Include("EventQuote.SelectedFoods")
                .Include("EventQuote.SelectedEquipments")
                .FirstOrDefault(b => b.EventBookingId == id);

            if (booking == null)
                return HttpNotFound();

            if (!User.IsInRole("Admin") && booking.UserEmail != User.Identity.Name)
                return new HttpUnauthorizedResult();

            return View(booking);
        }
        [Authorize]
        [Authorize]
        public ActionResult DownloadFinalInvoice(int bookingId)
        {
            var booking = db.EventBookings
                .Include("EventQuote.Venue")
                .Include("EventQuote.SelectedActivities")
                .Include("EventQuote.SelectedFoods")
                .FirstOrDefault(b => b.EventBookingId == bookingId);

            if (booking == null) return HttpNotFound();

            var duration = (booking.EventQuote.EventEndDateTime - booking.EventQuote.EventStartDateTime).TotalHours;
            var venueCost = booking.EventQuote.Venue.BaseRatePerHour * (decimal)duration;
            var activityCost = booking.EventQuote.SelectedActivities.Sum(a => a.PricePerGuest * booking.EventQuote.GuestCount);

            var foodSelections = db.EventFoodSelections
                .Include("Product")
                .Where(f => f.EventBookingId == bookingId)
                .ToList();

            var foodCost = foodSelections.Sum(s => s.Product.Price);
            var foodSummary = string.Join("<br/>", foodSelections.Select(f => $"🍽️ {f.Product.Name} – R {f.Product.Price:N2}"));

            var total = venueCost + activityCost + foodCost;

            var html = booking.EventQuote.SummaryHtml +
                       $"<br/><br/><strong>🎯 Activities:</strong> R {activityCost:N2}" +
                       $"<br/><strong>🍽️ Food:</strong><br/>{foodSummary}" +
                       $"<br/><br/><strong>💰 Final Total:</strong> R {total:N2}";

            var pdf = GeneratePDF(
                customerName: User.Identity.Name,
                email: User.Identity.Name,
                itemTitle: "Event Final Invoice",
                reference: "INVOICE-" + bookingId,
                summaryHtml: html,
                total: total
            );

            return File(pdf, "application/pdf", $"EventInvoice_{bookingId}.pdf");
        }

        [Authorize]
        public ActionResult FinalInvoice(int bookingId)
        {
            var booking = db.EventBookings
                .Include("EventQuote.Venue")
                .Include("EventQuote.SelectedActivities")
                .Include("EventQuote.SelectedFoods")
                .FirstOrDefault(b => b.EventBookingId == bookingId);

            if (booking == null) return HttpNotFound();

            var venue = booking.EventQuote.Venue;
            var durationHours = (booking.EventQuote.EventEndDateTime - booking.EventQuote.EventStartDateTime).TotalHours;
            var venueCost = venue.BaseRatePerHour * (decimal)durationHours;

            var activityCost = booking.EventQuote.SelectedActivities
                .Sum(a => a.PricePerGuest * booking.EventQuote.GuestCount);

            var estimatedFoodCost = booking.EventQuote.SelectedFoods
                .Sum(f => f.Price * booking.EventQuote.GuestCount);

            var foodSelections = db.EventFoodSelections
                .Include("Product")
                .Where(f => f.EventBookingId == bookingId)
                .ToList();

            var groupedFoodBreakdown = foodSelections
                .GroupBy(f => f.Product.Name)
                .ToDictionary(
                    g => g.Key,
                    g => new FoodBreakdownInfo
                    {
                        Quantity = g.Count(),
                        UnitPrice = g.First().Product.Price,
                        Total = g.Count() * g.First().Product.Price
                    });

            var actualFoodCost = groupedFoodBreakdown.Sum(g => g.Value.Total);

            var previousPayment = booking.AmountPaid;
            var finalTotal = venueCost + activityCost + actualFoodCost;
            var balanceDue = finalTotal - previousPayment;

            var model = new FinalInvoiceViewModel
            {
                Booking = booking,
                VenueCost = venueCost,
                ActivityCost = activityCost,
                EstimatedFoodCost = estimatedFoodCost,
                ActualFoodCost = actualFoodCost,
                FinalTotal = finalTotal,
                AmountPaid = previousPayment,
                BalanceDue = balanceDue,
                FoodBreakdown = groupedFoodBreakdown
            };

            return View(model);
        }

        public ActionResult MakePayment(int bookingId)
        {
            var booking = db.EventBookings.Find(bookingId);
            return View(booking);
        }

        [Authorize]
        public ActionResult SendRsvp(int id)
        {
            var booking = db.EventBookings
                .Include("EventQuote")
                .FirstOrDefault(b => b.EventBookingId == id && b.UserEmail == User.Identity.Name);

            if (booking == null || !booking.IsPaid || booking.EventQuote.EventType.ToString() != "Private")
            {
                TempData["Error"] = "RSVPs are only allowed for fully paid private events.";
                return RedirectToAction("MyBookings");
            }

            var sentCount = db.RSVPInvites.Count(r => r.EventBookingId == id);
            var maxGuests = booking.EventQuote.GuestCount;
            var remaining = maxGuests - sentCount;

            if (remaining <= 0)
            {
                TempData["Error"] = "❌ All guest slots for this event have been filled.";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.Remaining = remaining;

            var model = new SendRsvpViewModel
            {
                EventBookingId = id,
                EventTitle = booking.EventQuote?.Venue?.Name,
                Emails = ""
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendRsvp(SendRsvpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var booking = db.EventBookings
                .Include("EventQuote")
                .FirstOrDefault(b => b.EventBookingId == model.EventBookingId && b.UserEmail == User.Identity.Name);

            if (booking == null || !booking.IsPaid || booking.EventQuote.EventType.ToString() != "Private")
            {
                TempData["Error"] = "RSVPs are only allowed for fully paid private events.";
                return RedirectToAction("MyBookings");
            }

            var sentCount = db.RSVPInvites.Count(r => r.EventBookingId == model.EventBookingId);
            var maxGuests = booking.EventQuote.GuestCount;
            var remaining = maxGuests - sentCount;

            var emailList = model.Emails.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(e => e.Trim().ToLower())
                                         .Where(e => e.Contains("@"))
                                         .Distinct()
                                         .ToList();

            if (emailList.Count > remaining)
            {
                TempData["Error"] = $"❌ You can only invite {remaining} more guest(s).";
                return RedirectToAction("SendRsvp", new { id = model.EventBookingId });
            }

            foreach (var email in emailList)
            {
                var existingInvite = db.RSVPInvites
                    .FirstOrDefault(r => r.EventBookingId == booking.EventBookingId && r.GuestEmail.ToLower() == email);

                if (existingInvite == null)
                {
                    var token = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                    db.RSVPInvites.Add(new RSVPInvite
                    {
                        EventBookingId = booking.EventBookingId,
                        GuestEmail = email,
                        RSVPToken = token,
                        HasResponded = false
                    });
                }

                var link = Url.Action("Rsvp", "EventBooking", new { id = booking.EventBookingId, email = email }, protocol: Request.Url.Scheme);

                string body = $@"
Hello,<br/><br/>
You are invited to the event: <strong>{booking.EventQuote?.Venue?.Name}</strong><br/>
Date: {booking.EventQuote?.EventStartDateTime:f}<br/>
Venue: {booking.EventQuote?.Venue?.Name}<br/><br/>
Please confirm your attendance by clicking below:<br/>
<a href='{link}' target='_blank'>Confirm Attendance</a><br/><br/>
Regards,<br/>Durban Hotel Events Team";

                await new EmailController().SendEmailAsync(email, "You're Invited: Please RSVP", body);
            }

            db.SaveChanges();

            TempData["Success"] = "✅ RSVP invitations sent successfully.";
            return RedirectToAction("Details", new { id = model.EventBookingId });
        }

        public ActionResult Rsvp(int id, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return HttpNotFound("Email is required.");

            var invite = db.RSVPInvites
                           .Include("EventBooking.EventQuote.Venue")
                           .FirstOrDefault(i => i.EventBookingId == id && i.GuestEmail.ToLower() == email.ToLower());

            if (invite == null)
                return HttpNotFound("RSVP invitation not found.");

            var booking = invite.EventBooking;

            var model = new RSVPViewModel
            {
                RSVPToken = invite.RSVPToken,
                EventTitle = booking.EventQuote.Title,
                EventDate = booking.EventQuote.EventStartDateTime,
                VenueName = booking.EventQuote.Venue?.Name,
                Description = booking.EventQuote.Venue?.Description,
                Email = email,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RSVP(RSVPViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var invite = db.RSVPInvites
                           .Include("EventBooking.EventQuote.Venue")
                           .FirstOrDefault(i =>
                               i.RSVPToken == model.RSVPToken &&
                               !i.HasResponded);

            if (invite == null)
            {
                TempData["Error"] = "This RSVP invitation is invalid or has already been used.";
                return RedirectToAction("Index", "Home");
            }

            invite.Response = model.Response;
            invite.HasResponded = true;
            invite.ResponseDate = DateTime.Now;

            string token = null;
            string qrPath = null;
            string pdfPath = null;

            if (model.Response.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                token = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                invite.RSVPToken = token;

                var qrGenerator = new QRCoder.QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(token, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.QRCode(qrData);
                var qrBitmap = qrCode.GetGraphic(20);

                qrPath = Server.MapPath($"~/assets/images/qr_{token}.png");
                Directory.CreateDirectory(Server.MapPath("~/assets/images/"));
                qrBitmap.Save(qrPath);

                var ticket = new Ticket
                {
                    UserEmail = invite.GuestEmail,
                    EventBookingId = invite.EventBookingId,
                    TicketCode = token,
                    IssuedAt = DateTime.Now,
                    QRCodeImagePath = "/assets/images/qr_" + token + ".png"
                };

                db.Tickets.Add(ticket);
                db.Entry(invite).State = EntityState.Modified;
                db.SaveChanges();

                var pdfBytes = GenerateTicketPdf(
    invite.EventBooking.EventQuote.Venue.Name,
    invite.EventBooking.EventQuote.EventStartDateTime,
    invite.EventBooking.EventQuote.Venue.Description,
    token,
    invite.GuestEmail,
    qrPath 
);


                var fileName = $"Ticket_{token}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                pdfPath = Server.MapPath("~/assets/" + fileName);
                System.IO.File.WriteAllBytes(pdfPath, pdfBytes);

                var subject = $"🎟 Your Ticket for {invite.EventBooking.EventQuote.Venue.Name}";
                var viewLink = Url.Action("ViewTicket", "EventBooking", new { code = token }, protocol: Request.Url.Scheme);
                var foodSelectionLink = Url.Action("SelectFood", "EventFood", new { eventId = invite.EventBookingId,email= invite.GuestEmail }, protocol: Request.Url.Scheme);
                var body = $@"
<p>Dear Guest,</p>
<p>Thank you for confirming your attendance at <strong>{invite.EventBooking.EventQuote.Venue.Name}</strong>.</p>
<p><strong>Date:</strong> {invite.EventBooking.EventQuote.EventStartDateTime:f}</p>
<p><strong>Venue:</strong> {invite.EventBooking.EventQuote.Venue.Description}</p>
<p><strong>Your Ticket Code:</strong> <b>{token}</b></p>
<p><a href='{viewLink}'>Click here to view your ticket</a></p>
<p>We kindly request you to select your preferred meal in advance.</p>
<p><a href='{foodSelectionLink}' class='btn btn-primary'>Click here to select your meal</a></p>
<hr />
<p>Warm regards,<br/>Durban Hotel Events Team</p>";

                await new EmailController().SendEmailWithInlineImageAsync(
                    invite.GuestEmail,
                    subject,
                    body,
                    new[] { pdfPath },
                    qrPath,
                    "QrCodeImage"
                );


            }
            else
            {
                db.SaveChanges(); 
            }

            TempData["Success"] = model.Response == "Yes"
                ? "✅ Your RSVP has been recorded. Ticket sent to your email."
                : "📩 Your RSVP has been recorded. Thank you for your response.";

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult ViewTicket(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["Error"] = "❌ Invalid or missing ticket code.";
                return RedirectToAction("Index", "Home");
            }

            var ticket = db.Tickets
                           .Include("EventBooking.EventQuote.Venue")
                           .FirstOrDefault(t => t.TicketCode == code);

            if (ticket == null)
            {
                TempData["Error"] = "❌ Ticket not found.";
                return RedirectToAction("Index", "Home");
            }

            var invite = db.RSVPInvites
                           .FirstOrDefault(i => i.EventBookingId == ticket.EventBookingId &&
                                                i.GuestEmail == ticket.UserEmail &&
                                                i.RSVPToken == ticket.TicketCode);

            if (invite == null || !string.Equals(invite.Response, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "⚠️ You have not confirmed attendance or this RSVP is invalid.";
                return RedirectToAction("Index", "Home");
            }

            return View("ViewTicket", ticket);
        }

        public ActionResult PublicEventAnalytics(int id)
        {
            var booking = db.EventBookings.Include("EventQuote").FirstOrDefault(b => b.EventBookingId == id);
            if (booking == null || booking.EventQuote.EventType.ToString() != "Public")
                return HttpNotFound();

            var tickets = db.Tickets.Where(t => t.EventBookingId == id && t.IsPaid).ToList();

            var totalTicketsSold = tickets.Sum(t => t.Quantity ?? 0);
            var totalRevenue = tickets.Sum(t => t.AmountPaid);
            var ticketsScanned = tickets.Count(t => t.IsUsed);
            var ticketsPending = tickets.Count(t => !t.IsUsed);

            var feedbacks = db.EventFeedbacks.Where(f => f.EventBookingId == id).ToList();
            var totalRatings = feedbacks.Count;
            var avgRating = totalRatings > 0 ? feedbacks.Average(f => f.Rating) : 0;

            var breakdown = feedbacks
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new PublicEventAnalyticsViewModel
            {
                EventTitle = booking.EventQuote.Title,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TicketsScanned = ticketsScanned,
                TicketsPending = ticketsPending,
                AverageRating = avgRating,
                TotalRatings = totalRatings,
                RatingBreakdown = breakdown
            };

            ViewBag.EventBookingId = id;
            return View(viewModel);
        }

        public ActionResult PrivateEventAnalytics(int id)
        {
            var booking = db.EventBookings
                            .Include("EventQuote")
                            .FirstOrDefault(b => b.EventBookingId == id);

            if (booking == null || booking.EventQuote.EventType.ToString() != "Private")
                return HttpNotFound();

            var invites = db.RSVPInvites.Where(r => r.EventBookingId == id).ToList();

            var attendedEmails = db.Tickets
                                   .Where(t => t.EventBookingId == id && t.IsUsed)
                                   .Select(t => t.UserEmail.ToLower())
                                   .Distinct()
                                   .ToList();

            var attendedCount = invites.Count(i => attendedEmails.Contains(i.GuestEmail.ToLower()));

            var viewModel = new PrivateEventAnalyticsViewModel
            {
                EventTitle = booking.EventQuote.Title,
                InvitesSent = invites.Count(),
                ResponsesYes = invites.Count(i => i.Response == "Yes"),
                ResponsesNo = invites.Count(i => i.Response == "No"),
                NoResponse = invites.Count(i => !i.HasResponded),
                Attended = attendedCount
            };

            ViewBag.EventBookingId = id;
            return View(viewModel);
        }

      




        public async Task<ActionResult> PostToFacebook(int eventId)
        {
            var evt = db.EventBookings.Include("EventQuote.Venue").FirstOrDefault(e => e.EventBookingId == eventId);
            if (evt == null || evt.EventQuote.EventType.ToString() != "Public")
            {
                TempData["Error"] = "❌ Only public events can be posted to Facebook.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            string message = $"📣 New Public Event: {evt.EventQuote.Title}\n" +
                             $"🗓️ {evt.EventQuote.EventStartDateTime:dddd, dd MMMM yyyy 'at' hh:mm tt}\n" +
                             $"📍 {evt.EventQuote.Venue.Name}\n\n" +
                             $"🎟️ Book your tickets now: {Url.Action("Buy", "Ticket", new { id = evt.EventBookingId }, Request.Url.Scheme)}";

            string pageAccessToken = ConfigurationManager.AppSettings["FacebookPageAccessToken"];
            string pageId = ConfigurationManager.AppSettings["FacebookPageId"];
           

            
            var result = await FacebookPoster.PostEventAsync(pageAccessToken, pageId, message, null);

            if(result.Contains("Error"))
            {
                TempData["Error"] = "❌ Failed to post event to Facebook";
            }
            else
            {
                TempData["Success"] = "✅ Event posted to Facebook!";
            }
            

            return RedirectToAction("Details", "EventBooking", new { id = eventId });
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


        public static byte[] GenerateTicketPdf(
     string venueName,
     DateTime eventDate,
     string venueDescription,
     string ticketCode,
     string email,
     string qrImagePath = null)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A5, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var noteFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, BaseColor.GRAY);

                doc.Add(new Paragraph("🎫 RSVP Ticket", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                });

                void AddRow(string label, string value)
                {
                    var table = new PdfPTable(2) { WidthPercentage = 100 };
                    table.DefaultCell.Border = Rectangle.NO_BORDER;
                    table.AddCell(new Phrase(label, labelFont));
                    table.AddCell(new Phrase(value, valueFont));
                    doc.Add(table);
                    doc.Add(new Paragraph(" ", valueFont)); // Add spacing after each row
                }

                AddRow("Guest Email:", email);
                AddRow("Event Venue:", venueName);
                AddRow("Event Date:", eventDate.ToString("f"));
                AddRow("Venue Description:", venueDescription);
                AddRow("Ticket Code:", ticketCode);
                AddRow("Issued At:", DateTime.Now.ToString("f"));

                // Embed QR code
                if (!string.IsNullOrEmpty(qrImagePath))
                {
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph("QR Code for Entry", labelFont) { Alignment = Element.ALIGN_CENTER });

                    var qrImg = iTextSharp.text.Image.GetInstance(qrImagePath);
                    qrImg.Alignment = Element.ALIGN_CENTER;
                    qrImg.ScaleToFit(140f, 140f);
                    doc.Add(qrImg);

                    doc.Add(new Paragraph("Scan this QR code at the entrance.", valueFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 8,
                        SpacingAfter = 8
                    });
                }

                // Closing note
                doc.Add(new Paragraph("Please arrive 15 minutes before the event start time.", noteFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10
                });

                doc.Close();
                return ms.ToArray();
            }
        }
    }
}
