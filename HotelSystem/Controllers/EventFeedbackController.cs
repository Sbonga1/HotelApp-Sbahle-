using HotelSystem.Models;
using HotelSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class EventFeedbackController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        [AllowAnonymous]
        public ActionResult RateEvent(int id)
        {
            var evt = db.EventBookings.Include("EventQuote").FirstOrDefault(e => e.EventBookingId == id);
            if (evt == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var model = new RateEventViewModel
            {
                EventBookingId = id,
                EventTitle = evt.EventQuote.Title
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult RateEvent(RateEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ticket = db.Tickets.FirstOrDefault(t =>
                t.TicketCode == model.TicketCode &&
                t.EventBookingId == model.EventBookingId &&
                t.IsPaid);

            if (ticket == null)
            {
                ModelState.AddModelError("", "Invalid or unpaid ticket code.");
                return View(model);
            }

            bool alreadyRated = db.EventFeedbacks.Any(f => f.TicketCode == model.TicketCode);
            if (alreadyRated)
            {
                TempData["Error"] = "You have already rated this event.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            var feedback = new EventFeedback
            {
                EventBookingId = model.EventBookingId,
                TicketCode = model.TicketCode,
                Rating = model.Rating,
                Comment = model.Comment,
                SubmittedOn = DateTime.Now,
                Email = ticket.UserEmail
            };

            var seat = db.Seats.FirstOrDefault(s => s.OccupiedByEmail == ticket.UserEmail);
            seat.OccupiedByEmail = null;
            seat.ServedAt = null;
            db.EventFeedbacks.Add(feedback);
            db.SaveChanges();

            TempData["Success"] = "✅ Thank you for rating the event!";
            return RedirectToAction("Browse", "PublicEvent");
        }


    }
}