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
    public class ApartmentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Apartments
        [Authorize]
        public ActionResult Index()
        {

            return View(db.room.ToList());

        }

        // GET: Apartments/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Apartment apartment = db.room.Find(id);
            if (apartment == null)
            {
                return HttpNotFound();
            }

            return View(apartment);
        }


        // good
        [Authorize]
        public ActionResult Create(int? hotelID)
        {
            try
            {


                ViewData["hotelID"] = hotelID;
                return View();
            }
            catch (Exception)
            {

                return RedirectToAction("NoAccess", "Home");
            }



        }



        //// POST: Apartments/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Room,RoomType,Capacity,floor,description,rating,image,Status")] Apartment apartment, HttpPostedFileBase file)
        {

            if (ModelState.IsValid)
            {

                var fileName = Path.GetFileName(file.FileName); //getting only file name(ex-ganesh.jpg)  
                var ext = Path.GetExtension(file.FileName); //getting the extension(ex-.jpg)  

                string name = Path.GetFileNameWithoutExtension(fileName); //getting file name without extension  
                string myfile = name + "_" + apartment.ID + ext; //appending the name with id   
                var path = Path.Combine(Server.MapPath("~/assets/images"), myfile);  // store the file inside ~/project folder(Img) 

                apartment.image = myfile;
                db.room.Add(apartment);
                db.SaveChanges();

                file.SaveAs(path);
                return RedirectToAction("Index");

            }

            return View(apartment);

        }

        //GET: Apartments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (User.IsInRole("Admin"))
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Apartment apartment = db.room.Find(id);
                if (apartment == null)
                {
                    return HttpNotFound();
                }
                return View(apartment);
            }
            else
                return RedirectToAction("NoAccess", "Home");
        }

        // POST: Apartments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Room,Capacity,floor,description,rating,image,Status")] Apartment apartment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(apartment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");

            }
            return View(apartment);
        }

        // GET: Apartments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (User.IsInRole("Admin"))
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Apartment apartment = db.room.Find(id);
                if (apartment == null)
                {
                    return HttpNotFound();
                }
                return View(apartment);
            }
            else
                return RedirectToAction("NoAccess", "Home");
        }

        // POST: Apartments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Apartment apartment = db.room.Find(id);
            db.room.Remove(apartment);
            db.SaveChanges();
            return RedirectToAction("Apartments", "Hotels");
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