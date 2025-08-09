using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;

using System.Drawing;
using System.Web.Services.Description;
using System.Threading.Tasks;

namespace HotelSystem.Controllers
{
    public class ReservationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Reservations

        public ActionResult NoAccess()
        {
            ViewBag.Message = "No access";
            return View();
        }

        public ActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                var reservations = db.Reservations.Include(r => r.Apartment);
                return View(reservations.ToList());

            }
            else
                return RedirectToAction("NoAccess", "Reservations");
            
        }

        public ActionResult PendingRequests()
        {
            var reservations = db.Reservations.Where(x=>x.Status== "Received, Pending Payment").Include(r => r.Apartment);
            return View(reservations.ToList());
        }
        //User Panel
        public ActionResult MyReservations()
        {
            if (Request.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var currentUser = User.Identity.Name;
                var resev = (from c in db.Reservations
                             where c.Email == currentUser && c.Status == "Fee Settled"
                             select c.Status).FirstOrDefault();
                if (resev == "Fee Settled")
                {
                    Session["Status"] = "Shop";
                }
                Session["Refund"] = " ";
                Session["myReservations"] = "Reservation";
                string Email = User.Identity.Name;
                var reservations = db.Reservations.Include(r => r.Apartment);
                return View(reservations.Where(r => r.Email == Email).ToList());
            }
            else
                return RedirectToAction("NoAccess", "Reservations");
        }

        // GET: Reservations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

       

        public ActionResult Create(int ApartmentId, string CustomerUsername)
        {
            if (Request.IsAuthenticated && !User.IsInRole("Admin"))
            {
                string currentUser = User.Identity.Name;
                var details = db.UserDetails.Where(c => c.email == currentUser).FirstOrDefault();
                Reservation b = new Reservation()
                {
                    CustomerName = details.FirstName,
                    CustomerSurname = details.Surname,
                    Email = currentUser
                };

                
                ViewData["ApartmentId"] = ApartmentId;
                ViewData["CustomerUsername"] = CustomerUsername;
                ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room");

                return View(b);
            }
            else
                return RedirectToAction("NoAccess", "Reservations");
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
    [Bind(Include = "Id,Email,CustomerName,CustomerSurname,From,Time,End,Nights,Cost,RoomType,RoomNumber,Status,ApartmentId")]
    Reservation reservation,
    string promoCode)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
                return View(reservation);
            }

            if (reservation.From < DateTime.Today)
            {
                ViewBag.ErrorDate = "Check-In date cannot be earlier than today.";
                ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
                return View(reservation);
            }

            if (reservation.From.Date == DateTime.Today && reservation.Time < DateTime.Now)
            {
                ViewBag.ErrorTime = "Arrival time cannot be earlier than the current time.";
                ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
                return View(reservation);
            }

            reservation.Date = DateTime.Now;
            reservation.Status = "Received, Pending Payment";
            reservation.RoomType = reservation.GetRoomType();
            reservation.RoomNumber = reservation.GetRoom();

            double rate = double.Parse(reservation.GetRate());
            int nights = int.Parse(reservation.Nights);
            reservation.Cost = rate * nights;

            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var promo = db.PromoCodes.FirstOrDefault(p =>
                    p.Code == promoCode && !p.IsRedeemed && p.ExpiryDate >= DateTime.Now);

                if (promo != null)
                {
                    reservation.PromoAmt = promo.Amount;
                    promo.IsRedeemed = true;
                    db.Entry(promo).State = EntityState.Modified;
                }
                else
                {
                    TempData["PromoError"] = "Invalid or expired promo code.";
                    ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
                    return View(reservation);
                }
            }

            var room = db.room.FirstOrDefault(r => r.ID == reservation.ApartmentId);
            if (room != null)
            {
                room.booked = true;
                db.Entry(room).State = EntityState.Modified;
            }

            db.Reservations.Add(reservation);
            db.SaveChanges();

            var discount = db.Discounts.FirstOrDefault(d =>
                d.ApplicableItemType == "Room" &&
                d.IsActive &&
                d.StartDate <= DateTime.Now &&
                d.EndDate >= DateTime.Now &&
                d.MinimumSpend <= reservation.Cost &&
                d.ApplicableItemIds.Contains(reservation.ApartmentId.ToString()));

            if (discount != null)
            {
                var promoAmount = reservation.Cost * (discount.Percentage / 100.0);
                var generatedCode = GeneratePromoCode();

                db.PromoCodes.Add(new PromoCode
                {
                    Code = generatedCode,
                    Amount = Math.Round(promoAmount, 2),
                    ExpiryDate = DateTime.Now.AddMonths(1),
                    IsRedeemed = false,
                    UserId = reservation.Email
                });

                db.SaveChanges();

                string subject = "🎁 You've earned a new promo code!";
                string body = $@"
            <p>Hi {reservation.CustomerName},</p>
            <p>Thank you for booking with <strong>Durban Hotel</strong>.</p>
            <p>You've earned a new promo code for your next stay:</p>
            <ul>
                <li><strong>Code:</strong> <code>{generatedCode}</code></li>
                <li><strong>Value:</strong> R{promoAmount:F2}</li>
                <li><strong>Expires:</strong> {DateTime.Now.AddMonths(1):dd MMM yyyy}</li>
            </ul>
            <p>Apply this code during checkout to save!</p>
            <p>Kind regards,<br><strong>Durban Hotel Team</strong></p>";

                var emailController = new EmailController();
                await emailController.SendEmailAsync(reservation.Email, subject, body);

                TempData["NewPromo"] = $"🎁 Promo code {generatedCode} worth R{promoAmount:F2} sent to your email.";
            }

            await BookingEmail(reservation.Email);
            TempData["Success"] = "Reservation successful.";
            return RedirectToAction("MyReservations");
        }



        private string GeneratePromoCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }


        // GET: Reservations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (Request.IsAuthenticated && User.IsInRole("Admin"))
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Reservation reservation = db.Reservations.Find(id);
                if (reservation == null)
                {
                    return HttpNotFound();
                }
                ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
                return View(reservation);
            }
            else
                return RedirectToAction("NoAccess", "Reservations");
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Email,CustomerName,CustomerSurname,From,Time,Image,Nights,Cost,Status,RoomType,ApartmentId")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                if (reservation.From < DateTime.Today)
                {
                    string message = "";
                    message = "Check-In date cannot be earlier than today.";
                    ViewBag.ErrorDate = message;
                    return View(reservation);
                }
                else if (reservation.Time < DateTime.Today)
                {
                    string message = "";
                    message = "Arrival time cannot be earlier than current time zone.";
                    ViewBag.ErrorTime = message;
                    return View(reservation);
                }
                else
                {
                    ApplicationDbContext db = new ApplicationDbContext();

                    reservation.Status = reservation.GetStatus();
                    reservation.Cost = double.Parse(reservation.GetRate());
                    reservation.RoomType = reservation.GetRoom();
                    db.Entry(reservation).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("MyReservations");
                }
            }
            ViewBag.ApartmentId = new SelectList(db.room, "ID", "Room", reservation.ApartmentId);
            return View(reservation);
        }

    

        // GET: Reservations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (Request.IsAuthenticated && !User.IsInRole("Admin"))
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Reservation reservation = db.Reservations.Find(id);
                if (reservation == null)
                {
                    return HttpNotFound();
                }
                db.room.Where(a => a.ID == reservation.ApartmentId).FirstOrDefault().booked = false;
                return View(reservation);
            }
            else
                return RedirectToAction("NoAccess", "Reservations");
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Reservation reservation = db.Reservations.Find(id);
            db.Reservations.Remove(reservation);
            db.SaveChanges();

            ////Sending automated email to the customer 
            await CancellationEmail(reservation.Email);

            return RedirectToAction("MyReservations");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }



        [NonAction]
        public async Task BookingEmail(string Email)
        {
            using (var db = new ApplicationDbContext())
            {
                var reservation = db.Reservations.FirstOrDefault(r => r.Email == Email);

                if (reservation != null)
                {
                    string subject = "Reservation Successfully Submitted";

                    string body = $@"
                Congratulations {reservation.CustomerName} {reservation.CustomerSurname},<br/><br/>
                You have successfully submitted your reservation for a <b>{reservation.RoomType}</b> at <b>Durban Hotel</b>.<br/><br/>
                Please ensure you complete the payment before visiting and bring one of the following: 
                Passport, ID Card, or Driver's License.<br/>
                Your booking will be processed once payment is received. Please allow 24 hours for processing.<br/><br/>
                Regards,<br/>
                <b>Durban Hotel Team</b>
            ";

                    var emailController = new EmailController();
                    await emailController.SendEmailAsync(
                        reservation.Email,
                        subject,
                        body
                    );
                }
            }
        }



        [NonAction]
        public async Task CancellationEmail(string Email)
        {
            using (var db = new ApplicationDbContext())
            {
                var reservation = db.Reservations.FirstOrDefault(r => r.Email == Email);

                if (reservation != null)
                {
                    string subject = "Reservation Cancellation Notice";

                    string body = $@"
                Dear {reservation.CustomerName} {reservation.CustomerSurname},<br/><br/>
                This is to confirm that you have <strong>cancelled</strong> your reservation at <b>Durban Hotel</b>.<br/><br/>
                Please note that per our Terms & Conditions, cancellations are <strong>non-refundable</strong>.<br/><br/>
                We truly appreciate your interest in our hotel, and we look forward to welcoming you in the future.<br/><br/>
                Regards,<br/>
                <b>Durban Hotel Team</b>
            ";

                    var emailController = new EmailController();
                    await emailController.SendEmailAsync(
                        reservation.Email,
                        subject,
                        body
                    );
                }
            }
        }

        public ActionResult Fee()
        {
            return View();
        }


    }
}