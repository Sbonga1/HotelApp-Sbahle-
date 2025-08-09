using System;
using System.Collections.Generic;

using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HotelSystem.Models;

namespace HotelSystem.Controllers
{
    public class ResturantsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Resturants
        public ActionResult Index()
        {
            return View(db.Resturants.ToList());
        }

        // GET: Resturants/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Resturant resturant = db.Resturants.Find(id);
            if (resturant == null)
            {
                return HttpNotFound();
            }
            return View(resturant);
        }

        // GET: Resturants/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Resturants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Icon")] Resturant resturant, HttpPostedFileBase pictureFile)
        {
            if (ModelState.IsValid)
            {
                // Save the picture file on the server
                string pictureFileName = Guid.NewGuid().ToString() + Path.GetExtension(pictureFile.FileName);
                string picturePath = Path.Combine(Server.MapPath("~/assets/images/"), pictureFileName);
                pictureFile.SaveAs(picturePath);

                resturant.Icon = pictureFileName;

                db.Resturants.Add(resturant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(resturant);
        }

        // GET: Resturants/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Resturant resturant = db.Resturants.Find(id);
            if (resturant == null)
            {
                return HttpNotFound();
            }
            return View(resturant);
        }

        // POST: Resturants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Icon")] Resturant resturant)
        {
            if (ModelState.IsValid)
            {
                db.Entry(resturant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(resturant);
        }

        // GET: Resturants/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Resturant resturant = db.Resturants.Find(id);
            if (resturant == null)
            {
                return HttpNotFound();
            }
            return View(resturant);
        }

        // POST: Resturants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Resturant resturant = db.Resturants.Find(id);
            db.Resturants.Remove(resturant);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
