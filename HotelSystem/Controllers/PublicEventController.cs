using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class PublicEventController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        [AllowAnonymous]
        public ActionResult Browse()
        {
            var events = db.EventBookings
                .Include("EventQuote.Venue")
                .Where(e =>
                    e.IsPaid &&
                    e.EventQuote.EventType.ToString() == "Public" &&
                    e.EventQuote.EventStartDateTime > DateTime.Now)
                .OrderBy(e => e.EventQuote.EventStartDateTime)
                .ToList();

            string email = null;

            if (User.Identity.IsAuthenticated)
            {
                email = User.Identity.Name;
            }
            else if (Request.Cookies["GuestEmail"] != null)
            {
                email = Server.UrlDecode(Request.Cookies["GuestEmail"].Value).ToLower();
            }

            if (!string.IsNullOrEmpty(email))
            {
                // All user-occupied seats
                var userSeats = db.Seats
                    .Where(s => s.OccupiedByEmail == email && s.IsOccupied)
                    .ToList();

                // Events where user is seated
                var seatedEventIds = events
                    .Where(e => userSeats.Any(s => s.EventTable.VenueId == e.EventQuote.VenueId))
                    .Select(e => e.EventBookingId)
                    .ToDictionary(id => id, id => true);

                // Events where user's seat has been served
                var servedEventIds = events
                    .Where(e => userSeats.Any(s => s.EventTable.VenueId == e.EventQuote.VenueId && s.ServedAt != null))
                    .ToDictionary(id => id, id => true);

                // Events already rated
                var ratedEventIds = db.EventFeedbacks
                    .Where(f => f.Email == email)
                    .Select(f => f.EventBookingId)
                    .Distinct()
                    .ToList();

                // Events where food selection already made
                var foodSelectedEventIds = db.EventFoodSelections
                    .Where(f => f.UserEmail == email)
                    .Select(f => f.EventBookingId)
                    .Distinct()
                    .ToList();

                ViewBag.UserSeats = seatedEventIds;
                ViewBag.ServedSeats = servedEventIds;
                ViewBag.RatedEvents = ratedEventIds;
                ViewBag.FoodSelectedEvents = foodSelectedEventIds;
                ViewBag.Email = email;
            }
            else
            {
                ViewBag.UserSeats = new Dictionary<int, bool>();
                ViewBag.ServedSeats = new Dictionary<int, bool>();
                ViewBag.RatedEvents = new List<int>();
                ViewBag.FoodSelectedEvents = new List<int>();
                ViewBag.Email = null;
            }

            return View(events);
        }



    }
}