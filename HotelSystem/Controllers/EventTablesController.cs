using DocumentFormat.OpenXml.EMMA;
using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class EventTablesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult ClearAllSeats(int tableId, int eventId)
        {
            var table = db.EventTables.Include(t => t.Seats).FirstOrDefault(t => t.EventTableId == tableId);
            if (table == null)
            {
                TempData["Error"] = "Table not found.";
                return RedirectToAction("TablesForEvent", new { eventId });
            }

            foreach (var seat in table.Seats)
            {
                seat.IsOccupied = false;
                seat.OccupiedByEmail = null;
                seat.AssignedAt = null;
                seat.ServedAt = null;
                db.Entry(seat).State = EntityState.Modified;
            }

            db.SaveChanges();
            TempData["Success"] = "✅ All guests removed from seats.";
            return RedirectToAction("EventTablesSeats", new { eventId = eventId, tableId = tableId });
        }

        public ActionResult Index(int venueId)
        {
            var tables = db.EventTables
                .Include(t => t.Venue)
                .Where(t => t.VenueId == venueId)
                .ToList();

            ViewBag.VenueId = venueId;
            ViewBag.VenueName = db.Venues.Find(venueId)?.Name;
            return View(tables);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult EventTablesSeats(int eventId, int tableId)
        {
            // Get the event booking including its venue
            var booking = db.EventBookings
                .Include(b => b.EventQuote)
                .FirstOrDefault(b => b.EventBookingId == eventId);

            if (booking == null)
                return HttpNotFound();

            var venueId = booking.EventQuote.VenueId;

            // Find the table and make sure it belongs to the same venue as the event
            var table = db.EventTables
                .Include(t => t.Seats)
                .FirstOrDefault(t => t.EventTableId == tableId && t.VenueId == venueId);

            if (table == null)
            {
                return HttpNotFound(); // table not found or doesn't belong to this event
            }

            ViewBag.TableName = table.TableName;
            ViewBag.TableId = tableId;
            ViewBag.EventId = eventId;

            var seats = table.Seats?.ToList() ?? new List<Seat>();
            return View(seats);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult TablesForEvent(int eventId)
        {
            var booking = db.EventBookings
                .Include(b => b.EventQuote)
                .FirstOrDefault(b => b.EventBookingId == eventId);

            if (booking == null)
                return HttpNotFound();

            var venueId = booking.EventQuote.VenueId;

            var tables = db.EventTables
                .Where(t => t.VenueId == venueId)
                .ToList();

            ViewBag.EventId = eventId;
            ViewBag.VenueName = booking.EventQuote.Venue.Name;

            return View(tables);
        }

        public ActionResult Create(int venueId)
        {
            ViewBag.VenueId = venueId;
            return View(new EventTable { VenueId = venueId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventTable table, HttpPostedFileBase ImageUpload)
        {
            if (ModelState.IsValid)
            {
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    string uploadDir = Server.MapPath("~/Content/uploads/tables/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir); // ✅ only creates if missing
                    }

                    string fileName = Guid.NewGuid() + Path.GetExtension(ImageUpload.FileName);
                    string fullPath = Path.Combine(uploadDir, fileName);

                    ImageUpload.SaveAs(fullPath);
                    table.ImageUrl = "~/Content/uploads/tables/" + fileName;
                }

                db.EventTables.Add(table);
                db.SaveChanges();
                return RedirectToAction("Index", new { venueId = table.VenueId });
            }

            ViewBag.VenueId = table.VenueId;
            return View(table);
        }

        public ActionResult Edit(int id)
        {
            var table = db.EventTables.Find(id);
            if (table == null) return HttpNotFound();

            ViewBag.VenueId = table.VenueId;
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EventTable table, HttpPostedFileBase ImageUpload)
        {
            var existing = db.EventTables.Find(table.EventTableId);
            if (existing == null) return HttpNotFound();

            existing.TableName = table.TableName;

            if (ImageUpload != null && ImageUpload.ContentLength > 0)
            {
                string uploadDir = Server.MapPath("~/Content/uploads/tables/");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageUpload.FileName);
                string path = Path.Combine(uploadDir, fileName);
                ImageUpload.SaveAs(path);

                existing.ImageUrl = "~/Content/uploads/tables/" + fileName;
            }

            db.SaveChanges();
            return RedirectToAction("Index", new { venueId = table.VenueId });
        }


        public ActionResult Delete(int id)
        {
            var table = db.EventTables.Find(id);
            if (table == null) return HttpNotFound();
            return View(table);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var table = db.EventTables.Find(id);
            if (table == null) return HttpNotFound();

            int venueId = table.VenueId;

            // ✅ Delete image if it exists
            if (!string.IsNullOrEmpty(table.ImageUrl))
            {
                var absolutePath = Server.MapPath(table.ImageUrl);
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
            }

            db.EventTables.Remove(table);
            db.SaveChanges();

            return RedirectToAction("Index", new { venueId });
        }

    }
}
