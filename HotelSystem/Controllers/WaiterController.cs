using HotelSystem.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using HotelSystem.ViewModels;

namespace HotelSystem.Controllers
{
    public class WaiterController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Profile(int id)
        {
            var waiter = db.Waiters.Include(w => w.Assignments.Select(a => a.CustomerOrder))
                                   .FirstOrDefault(w => w.Id == id);

            if (waiter == null)
            {
                TempData["Error"] = "Waiter not found.";
                return RedirectToAction("WaiterList", "Admin");
            }

            return View(waiter);
        }


        [Authorize(Roles = "Waiter")]
        public ActionResult MyEventTables(int? eventId)
        {
            string email = User.Identity.Name;

            // Get all tables assigned to the current waiter
            var tables = db.EventTables
                .Include(t => t.Seats.Select(s => s.EventFoodSelections.Select(f => f.Product)))
                .Include(t => t.Venue)
                .Where(t => t.AssignedWaiterEmail == email)
                .ToList();

            // Extract venue IDs from waiter's tables
            var venueIds = tables.Select(t => t.VenueId).Distinct().ToList();

            // Get all related event bookings
            var events = db.EventBookings
                .Include(e => e.EventQuote.Venue)
                .Where(e => venueIds.Contains(e.EventQuote.VenueId))
                .ToList();

            // Optional: filter by specific eventId
            if (eventId.HasValue)
            {
                events = events.Where(e => e.EventBookingId == eventId.Value).ToList();
            }

            // Group tables under their corresponding event
            var grouped = events.Select(e => new WaiterEventTableGroup
            {
                EventBooking = e,
                Tables = tables.Where(t => t.VenueId == e.EventQuote.VenueId).ToList()
            })
            .Where(g => g.Tables.Any())
            .ToList();

            return View(grouped);
        }

        [Authorize(Roles = "Waiter")]
        public ActionResult MyEvents()
        {
            string email = User.Identity.Name;

            // Get events where the waiter is assigned to a table
            var eventBookings = db.EventTables
                .Where(t => t.AssignedWaiterEmail == email)
                .Select(t => t.VenueId)
                .Distinct()
                .ToList();

            var events = db.EventBookings
                .Include(e => e.EventQuote.Venue)
                .Where(e => eventBookings.Contains(e.EventQuote.VenueId))
                .ToList();

            return View(events);
        }

        [HttpGet]
        [Authorize(Roles = "Waiter")]
        public ActionResult ServeGuest(int seatId)
        {
            var seat = db.Seats
                .Include(s => s.EventTable.Venue)
                .FirstOrDefault(s => s.SeatId == seatId && s.IsOccupied);

            if (seat == null)
            {
                TempData["Error"] = "Seat not found or not occupied.";
                return RedirectToAction("MyEventTables", "Waiter");
            }

            var email = seat.OccupiedByEmail;

            var meal = db.EventFoodSelections
                .Include(f => f.Product)
                .FirstOrDefault(f => f.EventBooking.EventQuote.VenueId == seat.EventTable.VenueId && f.UserEmail == email);

            ViewBag.Email = email;
            ViewBag.SeatNumber = seat.SeatNumber;
            ViewBag.TableName = seat.EventTable.TableName;
            ViewBag.Venue = seat.EventTable.Venue.Name;
            ViewBag.SeatId = seat.SeatId;

            return View(meal);
        }

        [HttpPost]
        [Authorize(Roles = "Waiter")]
        [ValidateAntiForgeryToken]
        public ActionResult ServeGuestConfirmed(int seatId)
        {
            var seat = db.Seats
                .Include(s => s.EventTable.Venue)
                .FirstOrDefault(s => s.SeatId == seatId && s.IsOccupied);

            if (seat == null)
            {
                TempData["Error"] = "Seat not found or not occupied.";
                return RedirectToAction("MyEventTables", "Waiter");
            }

            var email = seat.OccupiedByEmail;

            var meal = db.EventFoodSelections
                .Include(f => f.Product)
                .FirstOrDefault(f => f.EventBooking.EventQuote.VenueId == seat.EventTable.VenueId && f.UserEmail == email);

            if (meal == null)
            {
                TempData["Error"] = "Guest has not selected a meal.";
                return RedirectToAction("ServeGuest", new { seatId });
            }

            seat.ServedAt = DateTime.Now;
            db.Entry(seat).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = $"Meal '{meal.Product.Name}' served to {email} at Seat {seat.SeatNumber}.";
            return RedirectToAction("MyEventTables", "Waiter");
        }


        [Authorize(Roles = "Waiter")]
        public ActionResult MyAssignments()
        {
            string email = User.Identity.Name;
            
            var assignments = db.WaiterAssignments
                .Where(a => a.Waiter.Email == email)
                .Include(a => a.CustomerOrder)
                .OrderByDescending(a => a.AssignedAt)
                .ToList();

            return View(assignments);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult AllAssignments()
        {
            var assignments = db.WaiterAssignments
                .Include(a => a.Waiter)
                .Include(a => a.CustomerOrder)
                .OrderByDescending(a => a.AssignedAt)
                .ToList();

            return View(assignments);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateTableNumber(int orderId, int tableNumber)
        {
            var order = db.CustomerOrders.Find(orderId);

            if (order != null)
            {
                order.TableNumber = tableNumber;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

                // Notify customer
                var customer = db.CustInfos.FirstOrDefault(c => c.Email == order.Email);
                if (customer != null)
                {
                    string subject = "🍽️ Your Table is Ready";
                    string body = $@"Hello {customer.Name},<br/><br/>
            Your Eat-In order (Order #{order.Id}) has been assigned to <strong>Table #{tableNumber}</strong>.<br/>
            A waiter will be with you shortly.<br/><br/>
            Thank you for dining with Durban Hotel.<br/><br/>
            Regards,<br/>Durban Hotel";

                    var emailController = new EmailController();
                    await emailController.SendEmailAsync(customer.Email, subject, body);
                }

                TempData["Success"] = $"Table #{tableNumber} assigned successfully and customer notified.";
            }
            else
            {
                TempData["Error"] = "Order not found.";
            }

            return RedirectToAction("MyAssignments");
        }


        public ActionResult MarkAsCompleted()
        { 
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MarkAsCompleted(string uniqueCode)
        {
            if (string.IsNullOrWhiteSpace(uniqueCode))
            {
                TempData["Error"] = "Please enter or scan a unique code.";
                return View();
            }

            if (!int.TryParse(uniqueCode, out int code))
            {
                TempData["Error"] = "Invalid code format.";
                return View();
            }

            var order = db.CustomerOrders.FirstOrDefault(o => o.UniqueCode == code);
            if (order == null)
            {
                TempData["Error"] = "No order found with this code.";
                return View();
            }

            if (order.Status == "Order Recived")
            {
                TempData["Info"] = "This order has already been marked as completed.";
                return View();
            }

            // Update status
            order.Status = "Order Recived";
            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();

            // Get customer info
            var customer = db.CustInfos.FirstOrDefault(c => c.Email == order.Email);
            if (customer != null)
            {
                string subject = "🍽️ Your Order Has Been Served";
                string body = $@"Hello {customer.Name},<br/><br/>
        Your food order (Order #{order.Id}) has been served on <strong>Table {order.TableNumber}</strong>.<br/><br/>
        We hope you enjoy your meal!<br/><br/>
        Regards,<br/>Durban Hotel";

                var emailController = new EmailController();
                await emailController.SendEmailAsync(customer.Email, subject, body);
            }

            TempData["Success"] = $"Order #{order.Id} for room {order.RoomNumber} has been confirmed and the customer has been notified.";
            return RedirectToAction("MyAssignments");
        }


        [HttpGet]
        public ActionResult ConfirmRoomService()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmRoomService(string uniqueCode)
        {
            if (string.IsNullOrWhiteSpace(uniqueCode))
            {
                TempData["Error"] = "Please enter a unique code.";
                return View();
            }

            if (!int.TryParse(uniqueCode, out int parsedCode))
            {
                TempData["Error"] = "Invalid code format.";
                return View();
            }

            var order = db.CustomerOrders.FirstOrDefault(o => o.UniqueCode == parsedCode);
            if (order == null)
            {
                TempData["Error"] = "Invalid code. No matching order found.";
                return View();
            }

            if (order.Status == "Order Recived")
            {
                TempData["Info"] = "Order has already been confirmed as delivered.";
                return View();
            }

            order.Status = "Order Recived";
            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();

            // Get customer and send notification
            var customer = db.CustInfos.FirstOrDefault(c => c.Email == order.Email);
            if (customer != null)
            {
                string subject = "🍽️ Room Service Delivered";
                string body = $@"Hello {customer.Name},<br/><br/>
        Your room service order (Order #{order.Id}) has been delivered to <strong>Room {order.RoomNumber}</strong>.<br/><br/>
        Thank you for using Durban Hotel Services.<br/><br/>
        Regards,<br/>Durban Hotel";

                var emailController = new EmailController();
                await emailController.SendEmailAsync(customer.Email, subject, body);
            }

            TempData["Success"] = $"Room service confirmed and customer notified for Room {order.RoomNumber}.";
            return RedirectToAction("MyAssignments", "Waiter");
        }




    }
}