using System;
using System.Collections.Generic;

using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using HotelSystem.Models;

namespace HotelSystem.Controllers
{
    public class OrderedProductsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: OrderedProducts
        public ActionResult Index()
        {

            var orderedProducts = db.Orderedproducts
                .Include(o => o.CustomerOrder)
                .Include(o => o.Product)
                
                .OrderBy(x => x.CustomerOrder.DateCreated)
                .ToList();

            // Group ordered products by date and time
            var groupedOrderedProducts = orderedProducts
                .GroupBy(op => Tuple.Create(op.CustomerOrder.DateCreated.Date, op.CustomerOrder.DateCreated.TimeOfDay))
                .ToList();

            return View(groupedOrderedProducts);
        }
       
        public ActionResult MyOrders()
        {
            var orderedProducts = db.Orderedproducts
                .Include(o => o.CustomerOrder)
                .Include(o => o.Product)
                .Where(x => x.CustomerOrder.Email == User.Identity.Name)
                .OrderBy(x => x.CustomerOrder.DateCreated)
                .ToList();

            // Group ordered products by date and time
            var groupedOrderedProducts = orderedProducts
                .GroupBy(op => Tuple.Create(op.CustomerOrder.DateCreated.Date, op.CustomerOrder.DateCreated.TimeOfDay))
                .ToList();

            return View(groupedOrderedProducts);
        }

        public ActionResult TrackOrder(int id)
        {
            var order = db.Orderedproducts
                .Include(o => o.CustomerOrder)
                .Include(o => o.Product).Where(x => x.CustomerOrderId == id).FirstOrDefault();
            return View(order);
        }

        // GET: OrderedProducts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderedProduct orderedProduct = db.Orderedproducts.Find(id);
            if (orderedProduct == null)
            {
                return HttpNotFound();
            }
            return View(orderedProduct);
        }

        // GET: OrderedProducts/Create
        public ActionResult Create()
        {
            ViewBag.CustomerOrderId = new SelectList(db.CustomerOrders, "Id", "FirstName");
            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name");
            return View();
        }

        // POST: OrderedProducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductId,CustomerOrderId,Quantity")] OrderedProduct orderedProduct)
        {
            if (ModelState.IsValid)
            {
                db.Orderedproducts.Add(orderedProduct);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CustomerOrderId = new SelectList(db.CustomerOrders, "Id", "FirstName", orderedProduct.CustomerOrderId);
            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name", orderedProduct.ProductId);
            return View(orderedProduct);
        }

        // GET: OrderedProducts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderedProduct orderedProduct = db.Orderedproducts.Find(id);
            if (orderedProduct == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerOrderId = new SelectList(db.CustomerOrders, "Id", "FirstName", orderedProduct.CustomerOrderId);
            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name", orderedProduct.ProductId);
            return View(orderedProduct);
        }

        // POST: OrderedProducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductId,CustomerOrderId,Quantity")] OrderedProduct orderedProduct)
        {
            if (ModelState.IsValid)
            {
                db.Entry(orderedProduct).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerOrderId = new SelectList(db.CustomerOrders, "Id", "FirstName", orderedProduct.CustomerOrderId);
            ViewBag.ProductId = new SelectList(db.Products, "Id", "Name", orderedProduct.ProductId);
            return View(orderedProduct);
        }

        // GET: OrderedProducts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderedProduct orderedProduct = db.Orderedproducts.Find(id);
            if (orderedProduct == null)
            {
                return HttpNotFound();
            }
            return View(orderedProduct);
        }

        // POST: OrderedProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OrderedProduct orderedProduct = db.Orderedproducts.Find(id);
            db.Orderedproducts.Remove(orderedProduct);
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
