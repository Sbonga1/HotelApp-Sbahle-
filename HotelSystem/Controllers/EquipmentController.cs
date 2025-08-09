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
    public class EquipmentController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Equipment
        public ActionResult Index()
        {
            return View(db.Equipment.ToList());
        }

        // GET: Equipment/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Equipment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Equipment equipment, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    ImageFile.SaveAs(path);
                    equipment.ImageUrl = "/assets/images/" + fileName;
                }

                db.Equipment.Add(equipment);
                db.SaveChanges();
                TempData["success"] = "✅ Equipment added successfully!";
                return RedirectToAction("Index");
            }
            return View(equipment);
        }

        // GET: Equipment/Edit/5
        public ActionResult Edit(int id)
        {
            var equipment = db.Equipment.Find(id);
            if (equipment == null) return HttpNotFound();
            return View(equipment);
        }

        // POST: Equipment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Equipment equipment, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existing = db.Equipment.Find(equipment.EquipmentId);
                if (existing == null) return HttpNotFound();

                existing.Name = equipment.Name;
                existing.QuantityAvailable = equipment.QuantityAvailable;
                existing.PricePerUnit = equipment.PricePerUnit;
                existing.Description = equipment.Description;

                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var path = Path.Combine(Server.MapPath("~/assets/images"), fileName);
                    ImageFile.SaveAs(path);
                    existing.ImageUrl = "/assets/images/" + fileName;
                }

                db.SaveChanges();
                TempData["success"] = "✅ Equipment updated successfully!";
                return RedirectToAction("Index");
            }
            return View(equipment);
        }

        // GET: Equipment/Delete/5
        public ActionResult Delete(int id)
        {
            var equipment = db.Equipment.Find(id);
            if (equipment == null) return HttpNotFound();
            return View(equipment);
        }

        // POST: Equipment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var equipment = db.Equipment.Find(id);
            db.Equipment.Remove(equipment);
            db.SaveChanges();
            TempData["success"] = "🗑️ Equipment deleted.";
            return RedirectToAction("Index");
        }
    }
}

    
