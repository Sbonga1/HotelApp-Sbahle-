using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class VenueController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Venues
        public ActionResult Venues()
        {
            var venues = db.Venues.ToList();
            return View(venues);
        }

        // GET: Admin/CreateVenue
        public ActionResult CreateVenue()
        {
            return View();
        }

        // POST: Admin/CreateVenue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateVenue(Venue venue, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    ImageFile.SaveAs(path);
                    venue.ImageUrl = "/assets/images/" + fileName;
                }

                db.Venues.Add(venue);
                db.SaveChanges();
                TempData["success"] = "✅ Venue added successfully!";
                return RedirectToAction("Venues");
            }

            return View(venue);
        }

        // GET: Admin/EditVenue/5
        public ActionResult EditVenue(int id)
        {
            var venue = db.Venues.Find(id);
            if (venue == null)
                return HttpNotFound();

            return View(venue);
        }

        // POST: Admin/EditVenue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditVenue(Venue venue, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existingVenue = db.Venues.Find(venue.VenueId);
                if (existingVenue == null)
                    return HttpNotFound();

                existingVenue.Name = venue.Name;
                existingVenue.Capacity = venue.Capacity;
                existingVenue.BasePrice = venue.BasePrice;
                existingVenue.Description = venue.Description;

                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    ImageFile.SaveAs(path);
                    existingVenue.ImageUrl = "/assets/images/" + fileName;
                }

                db.SaveChanges();
                TempData["success"] = "✅ Venue updated successfully!";
                return RedirectToAction("Venues");
            }

            return View(venue);
        }

        // GET: Admin/DeleteVenue/5
        public ActionResult DeleteVenue(int id)
        {
            var venue = db.Venues.Find(id);
            if (venue == null)
                return HttpNotFound();

            return View(venue);
        }

        // POST: Admin/DeleteVenue/5
        [HttpPost, ActionName("DeleteVenue")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteVenueConfirmed(int id)
        {
            var venue = db.Venues.Find(id);
            if (venue == null)
                return HttpNotFound();

            db.Venues.Remove(venue);
            db.SaveChanges();
            TempData["success"] = "🗑️ Venue deleted successfully!";
            return RedirectToAction("Venues");
        }

    }
}