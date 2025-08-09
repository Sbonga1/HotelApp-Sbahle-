using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class NotificationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpPost]
      
        public ActionResult MarkNotificationAsRead(int notificationId)
        {
            var notification = db.Notifications.Find(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                db.Entry(notification).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

    }

}