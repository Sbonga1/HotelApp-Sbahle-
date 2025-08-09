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
    public class TableCategoriesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: TableCategories
        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }
            else
            {
                var tableCategory = db.TableCategories.Where(x => x.ResturantId == id);
                ViewBag.ResName = db.Resturants.Where(x => x.Id == id).FirstOrDefault().Name;
                return View(tableCategory);
            }
        }
        



        public ActionResult UpdateCustInfo(double Total, int tableId, string Name, string Surname, DateTime Date, DateTime Time, bool UseAvailableAmount)
        {
            // Get customer info based on the logged-in user
            var custInfo = db.CustInfos.Where(x => x.Email == User.Identity.Name).FirstOrDefault();

            if (custInfo == null)
            {
                // Handle the case where customer info is not found
                return HttpNotFound("Customer information not found.");
            }

            // Update customer information
            custInfo.Surname = Surname;
            custInfo.Name = Name;
            custInfo.Date = Date;
            custInfo.Time = Time;
            custInfo.TableID = tableId;

            // If the customer chose to use their available balance
            if (UseAvailableAmount && custInfo.AccountBalance > 0)
            {
                double amountToDeduct = Math.Min(Total, custInfo.AccountBalance);
                Total -= amountToDeduct;

                custInfo.AccountBalance -= amountToDeduct;

            }

            db.Entry(custInfo).State = EntityState.Modified;
            db.SaveChanges();
            if(Total<=0)
            {
                Total=1;
            }

            return RedirectToAction("CreatePayment", "PayPal", new { Total = Total, tableId = tableId });
        }


        // GET: TableCategories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableCategory tableCategory = db.TableCategories.Find(id);
            if (tableCategory == null)
            {
                return HttpNotFound();
            }
            return View(tableCategory);
        }

        // GET: TableCategories/Create
        public ActionResult Create()
        {
            ViewBag.ResturantId = new SelectList(db.Resturants, "Id", "Name");
            return View();
        }

        // POST: TableCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,MaxGuests,Price,ResturantId,Icon")] TableCategory tableCategory, HttpPostedFileBase pictureFile)
        {
            if (ModelState.IsValid)
            {
                string pictureFileName = Guid.NewGuid().ToString() + Path.GetExtension(pictureFile.FileName);
                string picturePath = Path.Combine(Server.MapPath("~/assets/images/"), pictureFileName);
                pictureFile.SaveAs(picturePath);
                tableCategory.Icon = pictureFileName;
                tableCategory.Status = "Available";
                db.TableCategories.Add(tableCategory);
                db.SaveChanges();
                TempData["Success"] = "Table Created Successfully";
                return RedirectToAction("Create");
            }

            ViewBag.ResturantId = new SelectList(db.Resturants, "Id", "Name", tableCategory.ResturantId);
            return View(tableCategory);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableCategory tableCategory = db.TableCategories.Find(id);
            if (tableCategory == null)
            {
                return HttpNotFound();
            }
            ViewBag.ResturantId = new SelectList(db.Resturants, "Id", "Name", tableCategory.ResturantId);
            return View(tableCategory);
        }

        // POST: TableCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,MaxGuests,Price,ResturantId,Icon")] TableCategory tableCategory)
        {
            if (ModelState.IsValid)
            {

                db.Entry(tableCategory).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ResturantId = new SelectList(db.Resturants, "Id", "Name", tableCategory.ResturantId);
            return View(tableCategory);
        }

        // GET: TableCategories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableCategory tableCategory = db.TableCategories.Find(id);
            if (tableCategory == null)
            {
                return HttpNotFound();
            }
            return View(tableCategory);
        }

        // POST: TableCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TableCategory tableCategory = db.TableCategories.Find(id);
            db.TableCategories.Remove(tableCategory);
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
