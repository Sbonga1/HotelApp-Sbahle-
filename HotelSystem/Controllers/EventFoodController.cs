using HotelSystem.Models;
using HotelSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class EventFoodController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult SelectFood(int eventId, string email = null)
        {
            var eventBooking = db.EventBookings
                .Include("EventQuote")
                .Include("EventQuote.SelectedFoods")
                .FirstOrDefault(e => e.EventBookingId == eventId);

            if (eventBooking == null)
                return HttpNotFound();

            var foods = eventBooking.EventQuote.SelectedFoods.ToList();

            var model = new SelectFoodViewModel
            {
                GuestEmail = email,
                EventBookingId = eventId,
                AvailableFoods = foods,
                SelectedFoodId = foods.Count == 1 ? foods.First().Id : 0
            };

            return View(model);
        }
        [HttpPost]
        
        public ActionResult SelectFood(int EventBookingId, int SelectedFoodId,string GuestEmail =null)
        {
            var booking = db.EventBookings
                .Include("EventQuote")
                .Include("EventQuote.SelectedFoods")
                .FirstOrDefault(b => b.EventBookingId == EventBookingId);

            if (booking == null) return HttpNotFound();

            var food = db.Products.Find(SelectedFoodId);
            if (food == null) return HttpNotFound();
            if (Request.IsAuthenticated)
            {
                GuestEmail = User.Identity.Name;
            }
            if(GuestEmail == null){

               return RedirectToAction("Index","Home");
            }

            var existing = db.EventFoodSelections
                .FirstOrDefault(s => s.EventBookingId == EventBookingId && s.UserEmail == GuestEmail);

            if (existing != null)
                db.EventFoodSelections.Remove(existing);

            db.EventFoodSelections.Add(new EventFoodSelection
            {
                EventBookingId = EventBookingId,
                ProductId = SelectedFoodId,
                UserEmail = GuestEmail,
                SelectedAt = DateTime.Now
            });

            db.SaveChanges();
            TempData["Success"] = "✅ Your dish has been saved.";
            return RedirectToAction("Index", "Home");
        }


    }
}