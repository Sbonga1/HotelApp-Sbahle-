using System;
using System.Collections.Generic;

using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Org.BouncyCastle.Asn1.Tsp;
using HotelSystem.Models;
using System.Threading.Tasks;

namespace HotelSystem.Controllers
{
    public class TableReservationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Reservations
        public ActionResult Index(int id)
        {
            
            var reservations = db.TableReservations.Where(x=>x.TableCategory.ResturantId == id).Include(r => r.TableCategory);
            return View(reservations.ToList());
        }
        [HttpPost]
        public async Task<ActionResult> CancelReservation(int Id, string Reason, string OtherReason)
        {
            var custinfo = db.CustInfos.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            var reservation = db.TableReservations.Find(Id);
            var table = db.TableCategories.Find(reservation.TableCategoryId);
            if (reservation != null)
            {
                reservation.Status = "Cancelled";
                table.Status = "Available";

                reservation.CancellationReason = Reason == "Other" ? OtherReason : Reason;
               
                custinfo.AccountBalance += table.Price;
                db.Entry(reservation).State = EntityState.Modified;
                db.Entry(custinfo).State = EntityState.Modified;
                db.Entry(table).State = EntityState.Modified;

                try
                {
                    string subject = $"Table Reservation Cancellation Confirmation | Reservation ID: {reservation.Id}";

                    string emailBody = $@"
    <p>Dear {custinfo.Name},</p>
    <p>We regret to inform you that your reservation with ID <strong>{reservation.Id}</strong> has been <strong>successfully cancelled</strong> as per your request.</p>
    <p><strong>Details of the cancellation:</strong></p>
    <ul>
        <li><strong>Table:</strong> {reservation.TableCategory.Name}</li>
        <li><strong>Reservation Date:</strong> {reservation.ReservationDate:yyyy-MM-dd}</li>
        <li><strong>Reservation Time:</strong> {reservation.ReservationTime:hh\\:mm tt}</li>
        <li><strong>Reason for Cancellation:</strong> {reservation.CancellationReason}</li>
        <li><strong>Refund Amount:</strong> R{reservation.TableCategory.Price}</li>
    </ul>
    <p>The refund amount of <strong>R{reservation.TableCategory.Price}</strong> has been credited back to your account balance.</p>
    <p>If you have any questions or require further assistance, please do not hesitate to contact us.</p>
    <p>Thank you for choosing our service. We hope to serve you again in the future.</p>
    <br>
    <p>Best Regards,</p>
    <p><strong>The Event Zone Team</strong></p>
    ";

                    var emailController = new EmailController();
                    await emailController.SendEmailAsync(
                        recipientEmail: User.Identity.Name,
                        subject: subject,
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["Failure"] = "Failed to send email due to: " + ex.Message;
                    return RedirectToAction("Index", "Home");
                }


                db.SaveChanges();
            }
            TempData["Success"] = "Table Reservation Cancelled Successfully. The reservation amount has been added to your account balance.";
            return RedirectToAction("Index");
        }



        // GET: Reservations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableReservation reservation = db.TableReservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // GET: Reservations/Create
        public ActionResult Create()
        {
            ViewBag.TableCategoryId = new SelectList(db.TableCategories, "Id", "Name");
            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CustomerName,PhoneNumber,ReservationDate,NumberOfGuests,SpecialRequests,Status,TableCategoryId")] TableReservation reservation)
        {
            if (ModelState.IsValid)
            {
                db.TableReservations.Add(reservation);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TableCategoryId = new SelectList(db.TableCategories, "Id", "Name", reservation.TableCategoryId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableReservation reservation = db.TableReservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            ViewBag.TableCategoryId = new SelectList(db.TableCategories, "Id", "Name", reservation.TableCategoryId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CustomerName,PhoneNumber,ReservationDate,NumberOfGuests,SpecialRequests,Status,TableCategoryId")] TableReservation reservation)
        {
            if (ModelState.IsValid)
            {
                db.Entry(reservation).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.TableCategoryId = new SelectList(db.TableCategories, "Id", "Name", reservation.TableCategoryId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TableReservation reservation = db.TableReservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TableReservation reservation = db.TableReservations.Find(id);
            db.TableReservations.Remove(reservation);
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
