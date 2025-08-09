using HotelSystem.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class SeatsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(int tableId)
        {
            var seats = db.Seats.Where(s => s.EventTableId == tableId).ToList();
            ViewBag.TableId = tableId;
            var table = db.EventTables.Find(tableId);
            ViewBag.VenueId = table.VenueId;
            ViewBag.TableName = db.EventTables.Find(tableId)?.TableName;
            return View(seats);
        }

        public ActionResult Create(int tableId)
        {
            ViewBag.TableId = tableId;
            return View(new Seat { EventTableId = tableId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Seat seat)
        {
            if (ModelState.IsValid)
            {
                db.Seats.Add(seat);
                db.SaveChanges();
                return RedirectToAction("Index", new { tableId = seat.EventTableId });
            }

            ViewBag.TableId = seat.EventTableId;
            return View(seat);
        }

        public ActionResult Edit(int id)
        {
            var seat = db.Seats.Find(id);
            if (seat == null) return HttpNotFound();

            ViewBag.TableId = seat.EventTableId;
            return View(seat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Seat seat)
        {
            if (ModelState.IsValid)
            {
                db.Entry(seat).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { tableId = seat.EventTableId });
            }

            ViewBag.TableId = seat.EventTableId;
            return View(seat);
        }

        public ActionResult Delete(int id)
        {
            var seat = db.Seats.Find(id);
            if (seat == null) return HttpNotFound();
            return View(seat);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var seat = db.Seats.Find(id);
            int tableId = seat.EventTableId;

            db.Seats.Remove(seat);
            db.SaveChanges();
            return RedirectToAction("Index", new { tableId });
        }
    }
}
