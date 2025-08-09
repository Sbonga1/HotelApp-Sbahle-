using Microsoft.AspNet.Identity;
using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Index()
        {

            //Session["Status"] = "null";
            var currentUser = User.Identity.Name;
            var resev = (from c in db.Reservations
                         where c.Email == currentUser && c.Status == "Fee Settled"
                         select c.Status).FirstOrDefault();
            var resev2 = (from c in db.Reservations
                         where c.Email == currentUser && c.Status == "Checked IN"
                          select c.Status).FirstOrDefault();
            if (resev == "Fee Settled")
            {
                Session["Status"] = "Shop";
            }
            if(resev2 == "Checked IN")
            {
                Session["Brz"] = "Proceed";
            }

            Session["myReservations"] = "Reservation";

            var rooms = (from c in db.room
                         where c.booked == false
                         select c).ToList();
            return View(rooms.Take(4).ToList());
        }

        public ActionResult ResturantsHome()
        {
            var resturants = db.Resturants;
            return View(resturants.ToList());
        }
        public ActionResult Apartments()
        {
            return View(db.room.ToList());
        }

        public ActionResult Result()
        {
            return View(db.room.ToList());
        }
        public ActionResult Welcome()
        {
            return View();
        }
        public ActionResult Error()
        {
            return View();
        }
        [HttpPost]

        

        public ActionResult NoAccess()
        {
            ViewBag.Message = "No access";
            return View();
        }
    }
}