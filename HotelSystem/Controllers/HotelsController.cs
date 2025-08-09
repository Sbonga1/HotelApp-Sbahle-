using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class HotelsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Hotels
        public ActionResult Index()
        {
            return View(db.room.ToList());
        }
        public ActionResult Apartments()
        {
            var rooms = (from c in db.room
                         where c.booked == false
                         select c).ToList();
            return View(rooms.ToList());
        }
    }
}