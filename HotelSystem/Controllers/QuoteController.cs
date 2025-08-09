using HotelSystem.Models;
using HotelSystem.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class QuoteController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        [Authorize(Roles = "Admin")]
        public ActionResult QuotesToReview(string statusFilter = null)
        {
            IQueryable<EventQuote> quotesQuery = db.Quotes
                .Include("Venue")
                .OrderByDescending(q => q.CreatedAt); // Still ordered, but safely castable to IQueryable

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                quotesQuery = quotesQuery.Where(q => q.Status == statusFilter);
            }

            var quotes = quotesQuery.ToList();
            ViewBag.StatusFilter = statusFilter ?? "All";

            return View(quotes);
        }

        [Authorize]
        public ActionResult MyQuotes()
        {
            string currentEmail = User.Identity.Name;
            var quotes = db.Quotes
                .Include("Venue")
                .Where(q => q.UserEmail == currentEmail)
                .OrderByDescending(q => q.CreatedAt)
                .ToList();

            return View(quotes);
        }
        [Authorize]
        public ActionResult ViewQuoteSummary(int id)
        {
            var quote = db.Quotes
                .Include("Venue")
                .Include("SelectedActivities")
                .Include("SelectedFoods")
                .Include("SelectedEquipments")
                .FirstOrDefault(q => q.EventQuoteId == id);

            if (quote == null)
                return HttpNotFound();

            int durationHours = (int)(quote.EventEndDateTime - quote.EventStartDateTime).TotalHours;

            var model = new RequestQuoteViewModel
            {
                EventQuoteId = quote.EventQuoteId,
                GuestCount = quote.GuestCount,
                EventStartDateTime = quote.EventStartDateTime,
                EventEndDateTime = quote.EventEndDateTime,
                DurationHours = durationHours,
                VenueId = quote.VenueId,
                SelectedActivityIds = quote.SelectedActivities.Select(a => a.ActivityId).ToList(),
                SelectedFoodIds = quote.SelectedFoods.Select(f => f.Id).ToList(),
                SelectedEquipmentIds = quote.SelectedEquipments.Select(e => e.EquipmentId).ToList(),
                EstimatedTotal = quote.TotalCost,
                SummaryHtml = quote.SummaryHtml,
                QuoteStatus = quote.Status,
                IsFinalized = quote.IsFinalized,
                // Reference data for display
                VenueDetails = db.Venues.ToList(),
                Activities = db.Activities.ToList(),
                Foods = db.Products.ToList(),
                Equipments = db.Equipment.ToList()

            };
            ViewBag.Booking =db.EventBookings.Where(x=>x.EventQuoteId == id).FirstOrDefault();
            ViewBag.Back = "MyQuotes";
            return View("QuoteSummary", model);
        }
        [Authorize]
        [HttpGet]
        public ActionResult Request()
        {
            var model = new RequestQuoteViewModel
            {
                Venues = db.Venues.Select(v => new SelectListItem
                {
                    Text = v.Name,
                    Value = v.VenueId.ToString()
                }).ToList(),

                VenueDetails = db.Venues.ToList(), // Include full venue objects
                Activities = db.Activities.ToList(),
                Foods = db.Products.ToList(),
                Equipments = db.Equipment.ToList()
            };

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Request(RequestQuoteViewModel model)
        {
            model.VenueDetails = db.Venues.ToList();
            model.Activities = db.Activities.ToList();
            model.Foods = db.Products.ToList();
            model.Equipments = db.Equipment.ToList();

            model.Venues = db.Venues.Select(v => new SelectListItem
            {
                Text = v.Name,
                Value = v.VenueId.ToString()
            }).ToList();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join("<br/>", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return View(model);
            }

            var venue = model.VenueDetails.FirstOrDefault(v => v.VenueId == model.VenueId);
            if (venue == null)
            {
                TempData["Error"] = "Selected venue not found.";
                return View(model);
            }

            if (model.GuestCount > venue.Capacity)
            {
                TempData["Error"] = $"Guest count exceeds venue capacity ({venue.Capacity}).";
                return View(model);
            }

            double durationHours = (model.EventEndDateTime - model.EventStartDateTime).TotalHours;
            if (durationHours <= 0)
            {
                TempData["Error"] = "Event end time must be after start time.";
                return View(model);
            }

            // ✅ Detect booking conflict
            var clashingQuote = db.Quotes
                .Include("Venue")
                .FirstOrDefault(q =>
                    q.VenueId == model.VenueId &&
                    q.IsFinalized &&
                    (
                        (model.EventStartDateTime >= q.EventStartDateTime && model.EventStartDateTime < q.EventEndDateTime) ||
                        (model.EventEndDateTime > q.EventStartDateTime && model.EventEndDateTime <= q.EventEndDateTime) ||
                        (model.EventStartDateTime <= q.EventStartDateTime && model.EventEndDateTime >= q.EventEndDateTime)
                    )
                );

            if (clashingQuote != null)
            {
                var clashStart = clashingQuote.EventStartDateTime.ToString("f");
                var clashEnd = clashingQuote.EventEndDateTime.ToString("f");
                var suggestedStart = clashingQuote.EventEndDateTime.AddHours(1);
                var suggestedEnd = suggestedStart.Add(model.EventEndDateTime - model.EventStartDateTime);

                TempData["CustomError"] = $@"
🚫 <strong>Booking Clash Detected!</strong><br/>
<strong>{venue.Name}</strong> is already booked from <strong>{clashStart:dddd, dd MMM yyyy HH:mm}</strong> 
to <strong>{clashEnd:dddd, dd MMM yyyy HH:mm}</strong>.<br/><br/>
💡 <strong>Suggestion:</strong><br/>
You may book it from <strong>{suggestedStart:dddd, dd MMM yyyy HH:mm}</strong> 
to <strong>{suggestedEnd:dddd, dd MMM yyyy HH:mm}</strong> instead.";


                return View(model);
            }

            // ✅ Calculate total cost
            decimal total = venue.BaseRatePerHour * (decimal)durationHours;
            var activities = new List<Activity>();

            if (model.SelectedActivityIds != null)
            {
                activities = db.Activities
                               .Where(a => model.SelectedActivityIds.Contains(a.ActivityId))
                               .ToList();

                total += activities.Sum(a => a.PricePerGuest * model.GuestCount);
            }
            var foods = new List<Product>();
            if (model.SelectedFoodIds != null)
            {
                foods = db.Products.Where(f => model.SelectedFoodIds.Contains(f.Id)).ToList();
                total += foods.Sum(f => f.Price * model.GuestCount);

            }
            var equipments = new List<Equipment>();
            if(model.SelectedEquipmentIds!= null)
            {
                equipments = db.Equipment.Where(e => model.SelectedEquipmentIds.Contains(e.EquipmentId)).ToList();
                total += equipments.Sum(e => e.PricePerHour * (decimal)durationHours);
            }

            model.EstimatedTotal = total;

            model.SummaryHtml = $@"
<h5 class='text-primary'><i class='fa fa-info-circle me-1'></i>Quote Summary</h5>
<strong>📛 Title:</strong> {model.Title}<br/>
<strong>🏨 Venue:</strong> {venue.Name} at R {venue.BaseRatePerHour}/hr × {durationHours:F2} hrs = <strong> R {(venue.BaseRatePerHour * (decimal)durationHours)}</strong><br/>
<strong>👥 Guests:</strong> {model.GuestCount}<br/>
<strong>🕒 Duration:</strong> {durationHours:F2} hours<br/><br/>

<strong>🎯 Activities:</strong><br/>
{(activities.Any()
      ? string.Join("<br/>", activities.Select(a =>
          $"• {a.Name} (R {a.PricePerGuest} × {model.GuestCount} guests = R {(a.PricePerGuest * model.GuestCount)})"))
      : "None")}
<br/><br/>

<strong>🍽️ Food:</strong><br/>
{(foods.Any()
      ? string.Join("<br/>", foods.Select(f =>
          $"• {f.Name} (R {f.Price} × {model.GuestCount} = R {(f.Price * model.GuestCount)})"))
      : "None")}
<br/><br/>

<strong>🛠️ Equipment:</strong><br/>
{(equipments.Any()
      ? string.Join("<br/>", equipments.Select(e =>
          $"• {e.Name} (R {e.PricePerHour}/hr × {durationHours:F2} hrs = R {(e.PricePerHour * (decimal)durationHours)})"))
      : "None")}
<br/><hr/>

<h5 class='text-success'>💰 Total Estimate: R {total}</h5>";


            var eventQuote = new EventQuote
            {
                UserEmail = User.Identity.Name,
                Status = "Pending",
                EventStartDateTime = model.EventStartDateTime,
                EventEndDateTime = model.EventEndDateTime,
                GuestCount = model.GuestCount,
                VenueId = model.VenueId,
                TotalCost = total,
                IsFinalized = false,
                CreatedAt = DateTime.Now,
                SummaryHtml = model.SummaryHtml,
                SelectedActivities = activities,
                SelectedFoods = foods,
                SelectedEquipments = equipments,
                Title = model.Title,
                Description = model.Description,
                EventType = model.EventType
            };

            db.Quotes.Add(eventQuote);
            db.SaveChanges();

            model.DurationHours = (int)eventQuote.DurationHours;
            return View("QuoteSummary", model);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ReviewQuote(int id)
        {
            var quote = db.Quotes
                .Include("Venue")
                .Include("SelectedActivities")
                .Include("SelectedFoods")
                .Include("SelectedEquipments")
                .FirstOrDefault(q => q.EventQuoteId == id);

            if (quote == null)
                return HttpNotFound();

            var custinfo = db.CustInfos.FirstOrDefault(x => x.Email == quote.UserEmail);
            var durationHours = (quote.EventEndDateTime - quote.EventStartDateTime).TotalHours;



            var model = new AdminQuoteReviewViewModel
            {
                QuoteId = quote.EventQuoteId,
                ClientName = quote.UserEmail,
                ClientFullName = custinfo != null ? $"{custinfo.Name} {custinfo.Surname}" : quote.UserEmail,
                GuestCount = quote.GuestCount,
                DurationHours = durationHours,
                Venue = quote.Venue,
                SelectedActivities = quote.SelectedActivities.ToList(),
                SelectedFoods = quote.SelectedFoods.ToList(),
                SelectedEquipments = quote.SelectedEquipments.ToList(),
                EstimatedTotal = quote.TotalCost,
                Status = quote.Status,
                AdminNotes = quote.AdminNotes,
                EventStartDateTime = quote.EventStartDateTime,
                EventEndDateTime = quote.EventEndDateTime,
                Title = quote.Title,
                IsFinalized = quote.IsFinalized,
                EventType = quote.EventType.ToString(),
                CreatedAt = quote.CreatedAt
            };

            return View("ReviewQuote", model);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReviewQuote(AdminQuoteReviewViewModel model)
        {
            if (model.ActionType == null)
            {
                TempData["Error"] = "ActionType Null";
                return View(model);
            }
            var quote = db.Quotes
                .Include("Venue")
                .Include("SelectedActivities")
                .Include("SelectedFoods")
                .Include("SelectedEquipments")
                .FirstOrDefault(q => q.EventQuoteId == model.QuoteId);

            if (quote == null)
                return HttpNotFound();

            double durationHours = (quote.EventEndDateTime - quote.EventStartDateTime).TotalHours;
            decimal total = 0;

            total += quote.Venue.BaseRatePerHour * (decimal)durationHours;
            total += quote.SelectedActivities.Sum(a => a.PricePerGuest * quote.GuestCount);
            total += quote.SelectedFoods.Sum(f => f.Price * quote.GuestCount);
            total += quote.SelectedEquipments.Sum(e => e.PricePerHour * (decimal)durationHours);



            quote.Status = model.ActionType == "Decline" ? "Declined" : "Approved";
            quote.TotalCost = total;
            quote.AdminNotes = model.AdminNotes;
            quote.FinalizedAt = DateTime.Now;
            quote.IsFinalized = true;

            db.SaveChanges();

            var emailController = new EmailController();
            var subject = quote.Status == "Declined"
                ? $"❌ Your Quote #{quote.EventQuoteId} Was Declined"
                : $"✅ Your Quote #{quote.EventQuoteId} Has Been Approved";

            var emailBody = $@"
<p>Dear Customer,</p>
<p>Your event quote has been reviewed and marked as <strong>{quote.Status}</strong>. See summary below:</p>
<hr/>{quote.SummaryHtml}<hr/>
<p>To proceed, please log in to your account.</p>
<p>Regards,<br/>Hotel Events Team</p>";

            await emailController.SendEmailAsync(quote.UserEmail, subject, emailBody);

            TempData["Success"] = $"Quote #{quote.EventQuoteId} has been {quote.Status.ToLower()} and notification email sent.";
            return RedirectToAction("QuotesToReview");
        }

        [Authorize]
        public ActionResult ConfirmBooking(int id)
        {
            var quote = db.Quotes
                .Include("Venue")
                .Include("SelectedActivities")
                .Include("SelectedFoods")
                .Include("SelectedEquipments")
                .FirstOrDefault(q => q.EventQuoteId == id && q.UserEmail == User.Identity.Name && q.IsFinalized);

            if (quote == null)
                return HttpNotFound();

            var model = new ConfirmBookingViewModel
            {
                QuoteId = quote.EventQuoteId,
                SummaryHtml = quote.SummaryHtml,
                TotalCost = quote.TotalCost,
                EventStartDateTime = quote.EventStartDateTime,
                EventEndDateTime = quote.EventEndDateTime,
                GuestCount = quote.GuestCount,
                EventType = quote.EventType.ToString(),
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmBooking(ConfirmBookingViewModel model)
        {
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the terms to proceed.");
                return View(model);
            }

            var quote = db.Quotes
                .Include("Venue")
                .Include("SelectedActivities")
                .Include("SelectedFoods")
                .Include("SelectedEquipments")
                .FirstOrDefault(q => q.EventQuoteId == model.QuoteId && q.UserEmail == User.Identity.Name && q.IsFinalized);

            if (quote == null)
                return HttpNotFound();

            if (db.EventBookings.Any(b => b.EventQuoteId == quote.EventQuoteId))
            {
                TempData["Error"] = "⚠️ This quote has already been used to confirm a booking.";
                return RedirectToAction("MyBookings", "EventBooking");
            }

            var depositAmount = quote.TotalCost * 0.5m;

            var booking = new EventBooking
            {
                EventQuoteId = quote.EventQuoteId,
                UserEmail = quote.UserEmail ?? User.Identity.Name,
                Status = "Awaiting 50% Deposit",
                GuestPreferences = model.Preferences ?? "",
                ConfirmedAt = DateTime.Now,
                IsPaid = false,
                AmountPaid = 0,
                PaymentReference = null,
                DepositRequired = depositAmount
            };

            try
            {
                db.EventBookings.Add(booking);
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var failure in ex.EntityValidationErrors)
                {
                    foreach (var error in failure.ValidationErrors)
                    {
                        ModelState.AddModelError("", $"{error.PropertyName}: {error.ErrorMessage}");
                    }
                }

                // Repopulate model
                model.SummaryHtml = quote.SummaryHtml;
                model.TotalCost = quote.TotalCost;
                model.EventStartDateTime = quote.EventStartDateTime;
                model.EventEndDateTime = quote.EventEndDateTime;
                model.GuestCount = quote.GuestCount;
                model.EventType = quote.EventType.ToString();

                return View(model);
            }

            // Optional email
            var emailController = new EmailController();
            var subject = "✅ Booking Confirmed - 50% Deposit Required";
            var body = $@"
<p>Dear Customer,</p>
<p>Your booking has been recorded and is now awaiting a 50% deposit payment to proceed.</p>
<hr/>{quote.SummaryHtml}<hr/>
<p><strong>Amount Due Now:</strong> R {depositAmount:F2}</p>
<p>Please complete payment within 3 days to avoid cancellation.</p>
<p>Kind regards,<br/>Hotel Events Team</p>";

            await emailController.SendEmailAsync(quote.UserEmail, subject, body);

            TempData["Success"] = $"🎉 Your event has been confirmed. Please pay the deposit of R {depositAmount:F2}.";
            return RedirectToAction("MakePayment", "Payment", new { bookingId = booking.EventBookingId }); 
        }

    }

}