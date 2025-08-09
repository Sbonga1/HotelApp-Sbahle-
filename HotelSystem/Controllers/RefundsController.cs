using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text.pdf;
using iTextSharp.text;
using HotelSystem.Models;
using Microsoft.AspNet.Identity;
using System.Net.Mail;
using Org.BouncyCastle.Asn1.Ocsp;
using iTextSharp.text.pdf.parser;
using System.Web.Helpers;
using WebSharper.JavaScript;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;

namespace HotelSystem.Controllers
{
    public class RefundsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Refunds
        public ActionResult Index()
        {
            Session["myReservations"] = " ";
            Session["Refund"] = "Pay";
            return View(db.Refunds.ToList());
        }

        public ActionResult PendingRequests()
        {

            Session["myReservations"] = " ";
            Session["Refund"] = "Pay";
            var Refunds = db.Refunds.Where(x => x.RefundStatus == "Awaiting Approval");
            return View(Refunds.ToList());
        }

        public ActionResult MyRefunds()
        {
            string Email = User.Identity.Name;
            var reservations = db.Refunds;
            return View(reservations.Where(r => r.emailaddress == Email).ToList());
        }

        // GET: Refunds/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Refund refund = db.Refunds.Find(id);
            if (refund == null)
            {
                return HttpNotFound();
            }
            return View(refund);
        }

        // GET: Refunds/Create
        public ActionResult Create()
        {
            var currentUser = User.Identity.Name;

            var date = (from c in db.Reservations
                        where c.Email == currentUser && c.Status == "Fee Settled"
                        select c.Date).FirstOrDefault();

            var reservId = (from c in db.Reservations
                            where c.Email == currentUser && c.Status == "Fee Settled"
                            select c.Id).FirstOrDefault();

            var amtpaid = (from c in db.Reservations
                           where c.Email == currentUser && c.Status == "Fee Settled"
                           select c.Cost).FirstOrDefault();
            double CalcRefundFee()
            {
                return amtpaid * 0.05;
            }
            double CalcAmtTobePaid()
            {
                return amtpaid - CalcRefundFee();
            }

            Refund b = new Refund()
            {
                tobePaid = CalcAmtTobePaid(),
                RefundFee = CalcRefundFee(),
                ResevationDate = date.Date.ToShortDateString(),
                RefundDate = DateTime.Now.Date.ToShortDateString(),
               
                ReservationId = reservId,
                reservationAmtPaid = amtpaid,
                emailaddress = currentUser
            };

            return View(b);
        }

        // POST: Refunds/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "RefundId,ResevationDate,ReservationId,reservationAmtPaid,Reason,RefundDate,RefundStatus,emailaddress,RefundFee,tobePaid,signature")] Refund refund)
        {
            if (ModelState.IsValid)
            {
                refund.RefundStatus = "Awaiting Approval";

                var reservation = db.Reservations
                    .FirstOrDefault(x => x.Email == refund.emailaddress && x.Status == "Fee Settled");

                if (reservation == null)
                {
                    ModelState.AddModelError("", "Reservation not found or already refunded.");
                    return View(refund);
                }

                reservation.Status = "Refund Requested";
                db.Entry(reservation).State = EntityState.Modified;
                db.Refunds.Add(refund);
                db.SaveChanges();

                byte[] pdfBytes;
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A5, 36, 36, 36, 36); 
                    PdfWriter.GetInstance(document, memoryStream);
                    document.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                    var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                    var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                    var header = new Paragraph("Durban Hotel", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5
                    };
                    document.Add(header);

                    var subHeader = new Paragraph("Refund Summary", labelFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 15
                    };
                    document.Add(subHeader);

                    PdfPTable infoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };
                    infoTable.SetWidths(new float[] { 35f, 65f });

                    void AddRow(string label, string value)
                    {
                        infoTable.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 6 });
                        infoTable.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 6 });
                    }

                    AddRow("Refund ID:", refund.RefundId.ToString());
                    AddRow("Customer Email:", refund.emailaddress);
                    AddRow("Reservation ID:", refund.ReservationId.ToString());
                    AddRow("Reservation Date:", DateTime.Parse(refund.ResevationDate).ToString("dd MMM yyyy"));
                    AddRow("Refund Date:", DateTime.Parse(refund.RefundDate).ToString("dd MMM yyyy"));
                    AddRow("Refund Status:", refund.RefundStatus);
                    AddRow("Reason for Refund:", refund.Reason);

                    document.Add(infoTable);

                    var financeTitle = new Paragraph("Financial Summary", labelFont)
                    {
                        SpacingAfter = 5
                    };
                    document.Add(financeTitle);

                    PdfPTable financeTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };
                    financeTable.SetWidths(new float[] { 50f, 50f });

                    financeTable.AddCell(new PdfPCell(new Phrase("Amount Paid:", labelFont)) { Border = 0, PaddingBottom = 6 });
                    financeTable.AddCell(new PdfPCell(new Phrase(refund.reservationAmtPaid.ToString("R 0.00"), valueFont)) { Border = 0, PaddingBottom = 6 });

                    financeTable.AddCell(new PdfPCell(new Phrase("Refund Fee:", labelFont)) { Border = 0, PaddingBottom = 6 });
                    financeTable.AddCell(new PdfPCell(new Phrase(refund.RefundFee.ToString("R 0.00"), valueFont)) { Border = 0, PaddingBottom = 6 });

                    financeTable.AddCell(new PdfPCell(new Phrase("Final Refund Amount:", labelFont)) { Border = 0, PaddingBottom = 6 });
                    financeTable.AddCell(new PdfPCell(new Phrase(refund.tobePaid.ToString("R 0.00"), valueFont)) { Border = 0, PaddingBottom = 6 });

                    document.Add(financeTable);

                    var sigTitle = new Paragraph("Authorized Signature", labelFont)
                    {
                        SpacingBefore = 10,
                        SpacingAfter = 5
                    };
                    document.Add(sigTitle);

                    if (!string.IsNullOrEmpty(refund.signature))
                    {
                        var base64 = refund.signature.Substring(refund.signature.IndexOf(",") + 1);
                        byte[] signatureBytes = Convert.FromBase64String(base64);
                        var signatureImage = iTextSharp.text.Image.GetInstance(signatureBytes);
                        signatureImage.ScaleToFit(120f, 50f);
                        document.Add(signatureImage);
                    }

                    var footer = new Paragraph("\nThank you for choosing Durban Hotel.", valueFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 30
                    };
                    document.Add(footer);

                    document.Close();
                    pdfBytes = memoryStream.ToArray();
                }


                string fileName = $"Refund_{refund.RefundId}_{refund.emailaddress}.pdf";
                string filePath = Server.MapPath("~/") + fileName;
                System.IO.File.WriteAllBytes(filePath, pdfBytes);

                var customerName = reservation.CustomerName + " " + reservation.CustomerSurname;

                string subject = $"Refund Request Statement | Ref No.: {refund.RefundId}";
                string body = $"Hello {customerName},<br/><br/>Please see attached document for your refund request.";

                var emailController = new EmailController();
                await emailController.SendEmailAsync(
                    refund.emailaddress,
                    subject,
                    body + "<br/><br/>Thanks,<br/><b>Hotel Management</b>",
                    new[] { filePath }
                );

                return RedirectToAction("MyRefunds");
            }

            return View(refund);
        }

        public async Task<ActionResult> Approve(int id)
        {
            var refund = db.Refunds.Find(id);
            var custInfo = db.CustInfos.FirstOrDefault(x => x.Email == refund.emailaddress);

            if (refund == null || custInfo == null)
            {
                TempData["Error"] = "Refund or customer not found.";
                return RedirectToAction("Index");
            }

            custInfo.AccountBalance += refund.tobePaid;
            refund.RefundStatus = "Approved, Payment Made to Account";

            var transaction = new WalletTransaction
            {
                UserEmail = custInfo.Email,
                Date = DateTime.Now,
                Type = "Credit",
                Amount = refund.tobePaid,
                Description = $"Refund Credited | Ref: {refund.RefundId}"
            };
            db.WalletTransactions.Add(transaction);

            var promosToRemove = db.PromoCodes
                .Where(p => p.UserId == custInfo.Email && !p.IsRedeemed)
                .ToList();

            foreach (var promo in promosToRemove)
            {
                db.PromoCodes.Remove(promo); 
            }

            db.Entry(custInfo).State = System.Data.Entity.EntityState.Modified;
            db.Entry(refund).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            string subject = $"Refund Approved | Ref No.: {refund.RefundId}";
            string body = $@"
        Dear {custInfo.Name},<br/><br/>
        We are pleased to inform you that your refund request (Ref No: <b>{refund.RefundId}</b>) has been approved.<br/>
        An amount of <b>R {refund.tobePaid:F2}</b> has been credited to your hotel account.<br/><br/>
        Please note: all unused promo codes have been revoked due to this refund.<br/><br/>
        You can use your credit for any future bookings or purchases at our hotel.<br/><br/>
        <b>Thank you</b> for choosing Durban Hotel!<br/><br/>
        Warm regards,<br/>
        <b>Durban Hotel Team</b>
    ";

            var emailController = new EmailController();
            await emailController.SendEmailAsync(custInfo.Email, subject, body);

            TempData["Success"] = "Refund approved, promo codes removed, and customer account credited successfully.";
            return RedirectToAction("Index");
        }

        // GET: Refunds/Edit/5
        public ActionResult DeclineView(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Refund refund = db.Refunds.Find(id);
            var date = refund.RefundDate;
            var resrvationId = refund.ReservationId;
            var amtpaid = refund.reservationAmtPaid;
            var refundId = refund.RefundId;
            var resrvationDate = refund.ResevationDate;

            var email = refund.emailaddress;
            Session["GetEmail"] = email;
            if (refund == null)
            {
                return HttpNotFound();
            }
            Refund b = new Refund()
            {


                ResevationDate = resrvationDate,
                RefundDate = date,
                RefundId = refundId,
                ReservationId = resrvationId,
                reservationAmtPaid = amtpaid,
                emailaddress = email




            };



            return View(b);
        }

        [HttpPost]
        public async Task<ActionResult> DeclineView([Bind(Include = "RefundId,ResevationDate,ReservationId,reservationAmtPaid,Reason,RefundDate,RefundStatus,emailaddress,RefundFee,tobePaid,signature")] Refund refund)
        {
            refund.RefundStatus = "Declined";
            db.Entry(refund).State = EntityState.Modified;
            db.SaveChanges();

            var reservation = db.Reservations
                .FirstOrDefault(x => x.Email == refund.emailaddress && x.Status == "Refund Requested");

            if (reservation == null)
            {
                ModelState.AddModelError("", "Matching reservation not found.");
                return RedirectToAction("Index");
            }

            string fullName = $"{reservation.CustomerName} {reservation.CustomerSurname}";

            byte[] pdfBytes;
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A5, 36, 36, 36, 36); 
                PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                var header = new Paragraph("Durban Hotel", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5
                };
                document.Add(header);

                var subTitle = new Paragraph("Refund Declined Notification", labelFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                };
                document.Add(subTitle);

                
                PdfPTable table = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20
                };
                table.SetWidths(new float[] { 40f, 60f });

                void AddRow(string label, string value)
                {
                    table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 6 });
                    table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 6 });
                }

                AddRow("Refund ID:", refund.RefundId.ToString());
                AddRow("Customer Email:", refund.emailaddress);
                AddRow("Reservation ID:", refund.ReservationId.ToString());
                AddRow("Reservation Date:", DateTime.Parse(refund.ResevationDate).ToLongDateString());
                AddRow("Refund Date:", DateTime.Parse(refund.RefundDate).ToLongDateString());
                AddRow("Refund Status:", refund.RefundStatus);
                AddRow("Reason for Request:", refund.Reason);
                AddRow("Amount Paid:", refund.reservationAmtPaid.ToString("R 0.00"));

                document.Add(table);

                var sigTitle = new Paragraph("Authorized Signature", labelFont)
                {
                    SpacingAfter = 5
                };
                document.Add(sigTitle);

                if (!string.IsNullOrEmpty(refund.signature))
                {
                    var base64 = refund.signature.Substring(refund.signature.IndexOf(",") + 1);
                    byte[] signatureBytes = Convert.FromBase64String(base64);
                    var signatureImage = iTextSharp.text.Image.GetInstance(signatureBytes);
                    signatureImage.ScaleToFit(120f, 50f);
                    document.Add(signatureImage);
                }

                var note = new Paragraph(
                    "\nPlease note that your refund request was declined due to policy terms. " +
                    "For questions, contact durban_hotel324@outlook.com.",
                    valueFont)
                {
                    Alignment = Element.ALIGN_LEFT,
                    SpacingBefore = 20,
                    SpacingAfter = 10
                };
                document.Add(note);

                var footer = new Paragraph("Thank you for your understanding.", valueFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10
                };
                document.Add(footer);

                document.Close();
                pdfBytes = memoryStream.ToArray();
            }


            string fileName = $"Refund_Declined_{refund.RefundId}_{refund.emailaddress}.pdf";
            string filePath = Server.MapPath("~/") + fileName;
            System.IO.File.WriteAllBytes(filePath, pdfBytes);

            string subject = $"Refund Request Declined | Ref No.: {refund.RefundId}";
            string body = $"Hello {fullName},<br/><br/>Please see the attached document regarding your declined refund request.";

            var emailController = new EmailController();
            await emailController.SendEmailAsync(
                refund.emailaddress,
                subject,
                body + "<br/><br/>Thanks,<br/><b>Hotel Management</b>",
                new[] { filePath }
            );

            return RedirectToAction("Index");
        }

        // POST: Refunds/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RefundId,ResevationDate,ReservationId,reservationAmtPaid,Reason,RefundDate,emailaddress,RefundFee,signature")] Refund refund)
        {
            if (ModelState.IsValid)
            {
                
            }
            return View(refund);
        }
       
        // GET: Refunds/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var currentUser = User.Identity.Name;
            if(currentUser !=" ")
            {
                var resev = (from x in db.Reservations
                             where x.Email == currentUser
                             select x).FirstOrDefault();
                resev.Status = "Fee Settled";

                db.Entry(resev).State = EntityState.Modified;
                db.SaveChanges();

            }
           
            Refund refund = db.Refunds.Find(id);
            if (refund == null)
            {
                return HttpNotFound();
            }
            return View(refund);
        }

        // POST: Refunds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Refund refund = db.Refunds.Find(id);
            db.Refunds.Remove(refund);
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
