using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActivitiesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Activities
        public ActionResult Index()
        {
            var activities = db.Activities.ToList();
            return View(activities);
        }

        // GET: Activities/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Activity activity, HttpPostedFileBase IconImageFile)
        {
            if (ModelState.IsValid)
            {
                if (IconImageFile != null && IconImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(IconImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    IconImageFile.SaveAs(path);
                    activity.IconImageUrl = "/assets/images/" + fileName;
                }

                db.Activities.Add(activity);
                db.SaveChanges();
                TempData["success"] = "✅ Activity added successfully.";
                return RedirectToAction("Index");
            }

            return View(activity);
        }

        // GET: Activities/Edit/5
        public ActionResult Edit(int id)
        {
            var activity = db.Activities.Find(id);
            if (activity == null)
                return HttpNotFound();

            return View(activity);
        }

        // POST: Activities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Activity activity, HttpPostedFileBase IconImageFile)
        {
            if (ModelState.IsValid)
            {
                var existing = db.Activities.Find(activity.ActivityId);
                if (existing == null)
                    return HttpNotFound();

                existing.Name = activity.Name;
                existing.PricePerGuest = activity.PricePerGuest;

                if (IconImageFile != null && IconImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(IconImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    IconImageFile.SaveAs(path);
                    existing.IconImageUrl = "/assets/images/" + fileName;
                }

                db.SaveChanges();
                TempData["success"] = "✅ Activity updated successfully.";
                return RedirectToAction("Index");
            }

            return View(activity);
        }

        // GET: Activities/Delete/5
        public ActionResult Delete(int id)
        {
            var activity = db.Activities.Find(id);
            if (activity == null)
                return HttpNotFound();

            return View(activity);
        }

        // POST: Activities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var activity = db.Activities.Find(id);
            db.Activities.Remove(activity);
            db.SaveChanges();
            TempData["success"] = "🗑️ Activity deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}