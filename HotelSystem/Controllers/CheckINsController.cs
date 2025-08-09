using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNet.Identity;
using HotelSystem.Models;
using WebSharper.JavaScript;
using System.Web.Helpers;
using System.Threading.Tasks;

namespace HotelSystem.Controllers
{
    public class CheckINsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: CheckINs
        public ActionResult Index()
        {
            return View(db.CheckINs.ToList());
        }

        // GET: CheckINs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIN checkIN = db.CheckINs.Find(id);
            if (checkIN == null)
            {
                return HttpNotFound();
            }
            return View(checkIN);
        }
      
        // GET: CheckINs/Create
        public ActionResult Create()
        {
           
                CheckIN b = new CheckIN()
                {
                    CheckinDate = DateTime.Now.Date.ToShortDateString(),
                    CheckinTime = DateTime.Now.ToString("HH:mm")

                };
                return View(b);
           
        }
        [HttpGet]
        public JsonResult GetCustomerName(string reservationCode)
        {
            var reservation = db.Reservations.FirstOrDefault(r => r.CheckInCode == reservationCode);

            if (reservation != null)
            {
                return Json(new
                {
                    success = true,
                    firstName = reservation.CustomerName,
                    lastName = reservation.CustomerSurname
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        // POST: CheckINs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "ID,CheckinDate,CheckinTime,Signature,CustSignature,Name,Surname")] CheckIN checkIN)
        {
            if (ModelState.IsValid)
            {
                bool isFeeSettled = db.Reservations.Any(x => x.Id == checkIN.ID && x.Status == "Fee Settled");
                bool isAlreadyCheckedIn = db.Reservations.Any(x => x.Id == checkIN.ID && x.Status == "Checked IN");

                if (isFeeSettled)
                {
                    var reservation = db.Reservations.FirstOrDefault(x => x.Id == checkIN.ID && x.Status == "Fee Settled");

                    reservation.Status = "Checked IN";
                    checkIN.Room = reservation.RoomType;

                    db.Entry(reservation).State = EntityState.Modified;
                    db.CheckINs.Add(checkIN);
                    db.SaveChanges();

                    // Generate PDF
                    byte[] pdfBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        var document = new Document(PageSize.A5, 36, 36, 36, 36); // Margin: 0.5 inch
                        PdfWriter.GetInstance(document, memoryStream);
                        document.Open();

                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                        var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.DARK_GRAY);
                        var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                        // Header
                        var header = new Paragraph("Durban Hotel", titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 10
                        };
                        document.Add(header);

                        var subtitle = new Paragraph("Check-In Confirmation", labelFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 15
                        };
                        document.Add(subtitle);

                        // Details Table
                        var detailsTable = new PdfPTable(2)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 20
                        };
                        detailsTable.SetWidths(new float[] { 35, 65 });

                        void AddRow(string label, string value)
                        {
                            detailsTable.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 6 });
                            detailsTable.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, PaddingBottom = 6 });
                        }

                        AddRow("Reservation Number:", checkIN.ID.ToString());
                        AddRow("Name:", $"{checkIN.Name} {checkIN.Surname}");
                        AddRow("Room Type:", checkIN.Room);
                        AddRow("Check-In Date:", DateTime.Parse(checkIN.CheckinDate).ToLongDateString());
                        AddRow("Check-In Time:", checkIN.CheckinTime);

                        document.Add(detailsTable);

                        // Signature section
                        var signatureTitle = new Paragraph("Signatures", labelFont)
                        {
                            SpacingBefore = 10,
                            SpacingAfter = 10
                        };
                        document.Add(signatureTitle);

                        var signatureTable = new PdfPTable(2)
                        {
                            WidthPercentage = 100,
                            SpacingBefore = 10
                        };
                        signatureTable.SetWidths(new float[] { 50, 50 });

                        // Receptionist Signature
                        var receptionistBase64 = checkIN.Signature.Substring(checkIN.Signature.IndexOf(",") + 1);
                        byte[] receptionistSig = Convert.FromBase64String(receptionistBase64);
                        var receptionistImage = iTextSharp.text.Image.GetInstance(receptionistSig);
                        receptionistImage.ScaleToFit(150f, 60f);

                        // Customer Signature
                        var customerBase64 = checkIN.CustSignature.Substring(checkIN.CustSignature.IndexOf(",") + 1);
                        byte[] customerSig = Convert.FromBase64String(customerBase64);
                        var customerImage = iTextSharp.text.Image.GetInstance(customerSig);
                        customerImage.ScaleToFit(150f, 60f);

                        var recCell = new PdfPCell();
                        recCell.AddElement(new Paragraph("Receptionist", labelFont));
                        recCell.AddElement(receptionistImage);
                        recCell.Border = 0;

                        var custCell = new PdfPCell();
                        custCell.AddElement(new Paragraph("Customer", labelFont));
                        custCell.AddElement(customerImage);
                        custCell.Border = 0;

                        signatureTable.AddCell(recCell);
                        signatureTable.AddCell(custCell);

                        document.Add(signatureTable);

                        // Footer
                        var footer = new Paragraph("\nThank you for choosing Durban Hotel.", valueFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingBefore = 20
                        };
                        document.Add(footer);

                        document.Close();
                        pdfBytes = memoryStream.ToArray();
                    }


                    // Save PDF to disk
                    string fileName = $"CheckIn_{checkIN.ID}_{checkIN.Name}.pdf";
                    string filePath = Server.MapPath("~/") + fileName;
                    System.IO.File.WriteAllBytes(filePath, pdfBytes);

                    // Send Email using EmailController
                    string subject = $"Check-in Confirmation | Ref No.: {checkIN.ID}";
                    string body = $"Hello {reservation.CustomerName} {reservation.CustomerSurname},<br/><br/>Please see your attached check-in receipt below.";

                    var emailController = new EmailController();
                    await emailController.SendEmailAsync(
                        reservation.Email,
                        subject,
                        body + "<br/><br/>Thanks,<br/><b>Hotel Management</b>",
                        new[] { filePath }
                    );

                    Session["Brz"] = "Proceed";
                    ViewBag.Success = "Customer has been granted access to the hotel.";
                    return View(checkIN);
                }
                else if (isAlreadyCheckedIn)
                {
                    ViewBag.Error = "You have already checked in for this reservation number.";
                }
                else
                {
                    ViewBag.Error = "Invalid reservation or status. Please verify the reservation number.";
                }
            }

            return View(checkIN);
        }



        // GET: CheckINs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIN checkIN = db.CheckINs.Find(id);
            if (checkIN == null)
            {
                return HttpNotFound();
            }
            return View(checkIN);
        }

        // POST: CheckINs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,CheckinDate,CheckinTime,Signature,Name,Surname")] CheckIN checkIN)
        {
            if (ModelState.IsValid)
            {
                db.Entry(checkIN).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(checkIN);
        }

        // GET: CheckINs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIN checkIN = db.CheckINs.Find(id);
            if (checkIN == null)
            {
                return HttpNotFound();
            }
            return View(checkIN);
        }

        // POST: CheckINs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CheckIN checkIN = db.CheckINs.Find(id);
            db.CheckINs.Remove(checkIN);
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
