using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using HotelSystem.ViewModels;
using DocumentFormat.OpenXml.EMMA;

namespace HotelSystem.Controllers
{
    public class TableAssignmentController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        [Authorize(Roles = "Admin")]
        public ActionResult AssignWaiterToTable(int bookingId)
        {
            var booking = db.EventBookings
                .Include(b => b.EventQuote.Venue)
                .FirstOrDefault(b => b.EventBookingId == bookingId);

            if (booking == null)
                return HttpNotFound();

            var venueId = booking.EventQuote.VenueId;

            var tables = db.EventTables
                .Include(t => t.Seats)
                .Where(t => t.VenueId == venueId)
                .ToList();

            var waiters = db.Waiters
                .Select(w => new
                {
                    w.Email,
                    FullName = w.FullName
                })
                .ToList();

            var model = new TableWaiterAssignmentViewModel
            {
                BookingId = bookingId,
                Assignments = tables.Select(t => new TableAssignmentItem
                {
                    TableId = t.EventTableId,
                    TableName = t.TableName,
                    SeatCount = t.Seats?.Count ?? 0,
                    AssignedWaiterEmail = t.AssignedWaiterEmail
                }).ToList(),
                WaiterOptions = new SelectList(waiters, "Email", "FullName")
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult BulkAssignWaiters(TableWaiterAssignmentViewModel model)
        {
            if (model == null || model.Assignments == null || !model.Assignments.Any())
            {
                TempData["Error"] = "❌ No assignment data received.";
                return RedirectToAction("AssignWaiterToTable", new { bookingId = model.BookingId });
            }

            foreach (var assignment in model.Assignments)
            {
                var table = db.EventTables.FirstOrDefault(t => t.EventTableId == assignment.TableId);
                if (table != null)
                {
                    table.AssignedWaiterEmail = assignment.SelectedWaiterEmail;
                }
            }

            db.SaveChanges();
            TempData["Success"] = "✅ Waiters assigned to tables successfully.";
            return RedirectToAction("Details", "EventBooking", new { id = model.BookingId });
        }

        public ActionResult SelectTable(string email, int bookingId)
        {
            var eventQuote = db.EventBookings
                .Include(e => e.EventQuote)
                .FirstOrDefault(e => e.EventBookingId == bookingId)?.EventQuote;

            if (eventQuote == null) return HttpNotFound();

            var tables = db.EventTables
                .Include(t => t.Seats)
                .Where(t => t.VenueId == eventQuote.VenueId)
                .ToList();
            if (tables == null) return HttpNotFound();

            ViewBag.Email = email;
            ViewBag.BookingId = bookingId;
            ViewBag.VenueName = eventQuote.Venue.Name;
            if (bookingId > 0)
            {
                HttpCookie cookie = new HttpCookie("EventBookingId", bookingId.ToString());
                cookie.Expires = DateTime.Now.AddHours(2);
                Response.Cookies.Add(cookie);
            }

            return View(tables);
        }

        public ActionResult SelectSeat(int tableId, string email)
        {
            var seats = db.Seats
                .Where(s => s.EventTableId == tableId && !s.IsOccupied)
                .ToList();

            var table = db.EventTables.Include(t => t.Venue).FirstOrDefault(t => t.EventTableId == tableId);
            ViewBag.TableId = tableId;
            ViewBag.Email = email;
            ViewBag.TableName = table?.TableName;
            ViewBag.VenueId = table?.VenueId;

            return View(seats);
        }

        [HttpPost]
        public ActionResult AssignSeat(int seatId, string email)
        {
            var seat = db.Seats.Find(seatId);
            if (seat == null || seat.IsOccupied)
            {
                TempData["Error"] = "Seat not available.";
                return RedirectToAction("SelectSeat", new { tableId = seat?.EventTableId, email });
            }

            seat.IsOccupied = true;
            seat.OccupiedByEmail = email;
            seat.AssignedAt = DateTime.Now;
            db.SaveChanges();
            var table = db.EventTables.Find(seat.EventTableId);

            var bookingIdCookie = Request.Cookies["EventBookingId"];
            if (bookingIdCookie != null)
            {
                int bookingId = int.Parse(bookingIdCookie.Value);
                bookingIdCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(bookingIdCookie);

                TempData["Success"] = $"✅ Seat {seat.SeatNumber} assigned to {email}.";
                return RedirectToAction("Details", "EventBooking", new { id = bookingId });
            }

            TempData["Error"] = "Something Went Wrong!";
            return RedirectToAction("SelectTable", new { email, bookingId = table?.VenueId });


        }
    }

}