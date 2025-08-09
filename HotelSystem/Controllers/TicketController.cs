using HotelSystem.Helpers;
using HotelSystem.Models;
using HotelSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();



        public ActionResult Index(int id)
        {
            var booking = db.EventBookings
                            .Include("EventQuote.Venue")
                            .FirstOrDefault(b => b.EventBookingId == id);

            if (booking == null)
            {
                TempData["Error"] = "Event booking not found.";
                return RedirectToAction("Index", "EventBooking");
            }

            var types = db.TicketTypes
                          .Where(t => t.EventBookingId == id)
                          .ToList();

            ViewBag.EventId = id;
            ViewBag.EventTitle = booking.EventQuote?.Title ?? "Untitled Event";
            ViewBag.Capacity = booking.EventQuote?.GuestCount ?? 0;

            return View(types);
        }

        public ActionResult Create(int eventId)
        {
            var booking = db.EventBookings
                            .Include("EventQuote.Venue")
                            .FirstOrDefault(b => b.EventBookingId == eventId);

            if (booking == null)
            {
                TempData["Error"] = "Event booking not found.";
                return RedirectToAction("Index", "EventBooking");
            }

            // Total guest capacity
            int maxGuests = booking.EventQuote.GuestCount;

            // Total quantity already assigned to ticket types
            int totalAssigned = db.TicketTypes
                                  .Where(t => t.EventBookingId == eventId)
                                  .Sum(t => (int?)t.Quantity) ?? 0;

            // Remaining
            int remaining = maxGuests - totalAssigned;

            ViewBag.Event = booking;
            ViewBag.RemainingCapacity = remaining;

            return View(new TicketType
            {
                EventBookingId = booking.EventBookingId
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TicketType ticketType)
        {
            var booking = db.EventBookings
                            .Include("EventQuote")
                            .FirstOrDefault(e => e.EventBookingId == ticketType.EventBookingId);

            if (booking == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index", "EventBooking");
            }

            // Calculate remaining capacity
            int maxGuests = booking.EventQuote.GuestCount;
            int totalExisting = db.TicketTypes
                                  .Where(t => t.EventBookingId == ticketType.EventBookingId)
                                  .Sum(t => (int?)t.Quantity) ?? 0;
            int remaining = maxGuests - totalExisting;

            // Validation
            if (ticketType.Quantity > remaining)
            {
                ModelState.AddModelError("Quantity", $"Cannot exceed remaining capacity of {remaining} guest(s).");
                ViewBag.Event = booking;
                ViewBag.RemainingCapacity = remaining;
                return View(ticketType);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Event = booking;
                ViewBag.RemainingCapacity = remaining;
                return View(ticketType);
            }

            ticketType.Event = booking;
            db.TicketTypes.Add(ticketType);
            db.SaveChanges();

            TempData["Success"] = "✅ Ticket type created successfully.";
            return RedirectToAction("Details", "EventBooking", new { id = ticketType.EventBookingId });
        }

        public ActionResult Edit(int id)
        {
            var type = db.TicketTypes.Find(id);
            if (type == null)
                return HttpNotFound();

            var booking = db.EventBookings.Include("EventQuote")
                                          .FirstOrDefault(b => b.EventBookingId == type.EventBookingId);

            if (booking != null)
            {
                ViewBag.MaxGuests = booking.EventQuote?.GuestCount ?? 0;
            }

            return View(type);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TicketType model)
        {
            if (!ModelState.IsValid)
            {
                var booking = db.EventBookings.Include("EventQuote")
                                              .FirstOrDefault(b => b.EventBookingId == model.EventBookingId);

                if (booking != null)
                {
                    ViewBag.MaxGuests = booking.EventQuote?.GuestCount ?? 0;
                }

                return View(model);
            }

            var existing = db.TicketTypes.Find(model.TicketTypeId);
            if (existing == null)
            {
                return HttpNotFound();
            }

            // Validate that quantity doesn't exceed remaining capacity
            int totalOtherQuantities = db.TicketTypes
                                         .Where(t => t.EventBookingId == existing.EventBookingId && t.TicketTypeId != model.TicketTypeId)
                                         .Sum(t => (int?)t.Quantity) ?? 0;

            var bookingDetails = db.EventBookings.Include("EventQuote").FirstOrDefault(b => b.EventBookingId == existing.EventBookingId);
            int max = bookingDetails?.EventQuote?.GuestCount ?? 0;

            if ((totalOtherQuantities + model.Quantity) > max)
            {
                ModelState.AddModelError("Quantity", $"Total quantity exceeds allowed guest count ({max}).");
                ViewBag.MaxGuests = max;
                return View(model);
            }

            // Update allowed fields
            existing.Name = model.Name;
            existing.Price = model.Price;
            existing.Description = model.Description;
            existing.Quantity = model.Quantity;

            db.SaveChanges();

            TempData["Success"] = "Ticket type updated successfully.";
            return RedirectToAction("Index", "Ticket", new { id = existing.EventBookingId });
        }


        public ActionResult Delete(int id)
        {
            var type = db.TicketTypes.Find(id);
            if (type == null)
                return HttpNotFound();

            return View(type);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var type = db.TicketTypes.Find(id);
            if (type == null)
                return HttpNotFound();

            int? eventBookingId = type.EventBookingId; // retain associated event for redirect

            db.TicketTypes.Remove(type);
            db.SaveChanges();

            TempData["Success"] = "🗑 Ticket type deleted successfully.";
            return RedirectToAction("Details", "EventBooking", new { id = eventBookingId });
        }

        [HttpGet]
        public ActionResult Buy(int id)
        {
            var evt = db.EventBookings
                        .Include("EventQuote.Venue")
                        .FirstOrDefault(e => e.EventBookingId == id);

            if (evt == null || evt.EventQuote.EventType.ToString() != "Public")
                return HttpNotFound();

            var ticketTypes = db.TicketTypes
                                .Where(t => t.EventBookingId == id)
                                .ToList()
                                .Select(t => new TicketTypeWithStats
                                {
                                    TicketTypeId = t.TicketTypeId,
                                    Name = t.Name,
                                    Price = t.Price,
                                    Quantity = t.Quantity,
                                    Description = t.Description,
                                    SoldQuantity = db.Tickets
                                        .Where(x => x.TicketType == t.Name && x.EventId == id && x.IsPaid)
                                        .Sum(x => (int?)x.Quantity) ?? 0
                                }).ToList();

            var model = new BuyTicketViewModel
            {
                EventBookingId = evt.EventBookingId,
                EventTitle = evt.EventQuote.Title,
                EventDate = evt.EventQuote.EventStartDateTime,
                Venue = evt.EventQuote.Venue?.Name,
                AvailableTicketTypes = ticketTypes
            };

            return View(model);
        }
        [HttpPost]
        public ActionResult Buy(BuyTicketViewModel model)
        {
            var ticketType = db.TicketTypes.FirstOrDefault(t => t.TicketTypeId == model.SelectedTicketTypeId);
            if (ticketType == null || model.Quantity <= 0)
            {
                ModelState.AddModelError("", "Invalid ticket selection or quantity.");
            }
            else
            {
                // Calculate remaining quantity
                int sold = db.Tickets
                             .Where(t => t.TicketType == ticketType.Name && t.EventId == model.EventBookingId && t.IsPaid)
                             .Sum(t => (int?)t.Quantity) ?? 0;

                int remaining = (ticketType.Quantity) - sold;

                if (model.Quantity > remaining)
                {
                    ModelState.AddModelError("", $"Only {remaining} tickets remaining for {ticketType.Name}.");
                }
            }

            if (!ModelState.IsValid)
            {
                var booking = db.EventBookings
                                .Include("EventQuote.Venue")
                                .FirstOrDefault(e => e.EventBookingId == model.EventBookingId);

                if (booking != null)
                {
                    model.EventTitle = booking.EventQuote.Title;
                    model.EventDate = booking.EventQuote.EventStartDateTime;
                    model.Venue = booking.EventQuote.Venue?.Name;

                    var ticketTypes = db.TicketTypes
                                        .Where(t => t.EventBookingId == booking.EventBookingId)
                                        .ToList()
                                        .Select(t => new TicketTypeWithStats
                                        {
                                            TicketTypeId = t.TicketTypeId,
                                            Name = t.Name,
                                            Price = t.Price,
                                            Quantity = t.Quantity,
                                            Description = t.Description,
                                            SoldQuantity = db.Tickets
                                                             .Where(x => x.TicketType == t.Name && x.EventId == model.EventBookingId && x.IsPaid)
                                                             .Sum(x => (int?)x.Quantity) ?? 0
                                        }).ToList();

                    model.AvailableTicketTypes = ticketTypes;
                }

                return View(model);
            }

            // Valid purchase - set TempData for payment
            TempData["TicketPurchase"] = model;
            TempData["Total"] = ticketType.Price * model.Quantity;
            TempData["TicketTypeName"] = ticketType.Name;
            TempData["PricePerTicket"] = ticketType.Price;

         
            return RedirectToAction("EnterBuyerInfo", "Ticket");

        }
        [HttpGet]
        public ActionResult EnterBuyerInfo()
        {
            var model = TempData["TicketPurchase"] as BuyTicketViewModel;
            if (model == null)
            {
                TempData["Error"] = "Ticket purchase session expired.";
                return RedirectToAction("Browse", "PublicEvent");
            }

            string userEmail = null;
            if (User.Identity.IsAuthenticated)
            {
                userEmail = User.Identity.Name;
            }
            else if (Request.Cookies["GuestEmail"] != null)
            {
                userEmail = Server.UrlDecode(Request.Cookies["GuestEmail"].Value).ToLower();
            }

            var customer = !string.IsNullOrEmpty(userEmail)
                ? db.CustInfos.FirstOrDefault(x => x.Email == userEmail)
                : null;

            var buyerInfo = new EnterBuyerInfoViewModel
            {
                EventBookingId = model.EventBookingId,
                SelectedTicketTypeId = model.SelectedTicketTypeId,
                Quantity = model.Quantity,
                TicketTypeName = TempData["TicketTypeName"]?.ToString(),
                PricePerTicket = (decimal)(TempData["PricePerTicket"] ?? 0),
                BuyerEmail = userEmail,
                BuyerName = customer != null ? (customer.Name + " " + customer.Surname) : null
            };

            TempData["TicketPurchase"] = model;
            TempData["TicketTypeName"] = buyerInfo.TicketTypeName;
            TempData["PricePerTicket"] = buyerInfo.PricePerTicket;

            return View(buyerInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmBuyerInfo(EnterBuyerInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EnterBuyerInfo", model);
            }

            TempData["BuyerName"] = model.BuyerName;
            TempData["BuyerEmail"] = model.BuyerEmail;
            TempData["TicketPurchaseConfirmed"] = model;

            return RedirectToAction("PayForTicket", "Payment");
        }

        public ActionResult MyTickets()
        {
            string email = User.Identity.Name;

            var tickets = db.Tickets
                .Where(t => t.UserEmail == email && t.IsPaid && t.EventId != null) // Only Public Event Tickets
                .OrderByDescending(t => t.PurchaseDate)
                .ToList();

            return View(tickets);
        }

        public ActionResult Scan(int id)
        {
            ViewBag.EventBookingId = id;
            return View();
        }

        [HttpPost]
        public JsonResult ValidateTicketCode(int eventBookingId, string code)
        {
            var ticket = db.Tickets.FirstOrDefault(t =>
                t.EventBookingId == eventBookingId &&
                t.TicketCode.ToLower() == code.ToLower() &&
                !t.IsUsed);

            if (ticket == null)
            {
                return Json(new { success = false, message = "❌ Ticket is either used, unpaid, or invalid." });
            }

            var eventType = db.EventBookings
                              .Include("EventQuote")
                              .Where(e => e.EventBookingId == eventBookingId)
                              .Select(e => e.EventQuote.EventType)
                              .FirstOrDefault()
                              .ToString();

            if (eventType == "Public" && !ticket.IsPaid)
            {
                return Json(new { success = false, message = "❌ Ticket is unpaid." });
            }
            var eventbooking = db.EventBookings.Find(ticket.EventBookingId);
            ticket.IsUsed = true;
            ticket.UsedAt = DateTime.Now;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "✅ Ticket is valid.",
                email = ticket.UserEmail,
                type = ticket.TicketType,
                event_tittle = eventbooking.EventQuote.Title
            });
        }

        public FileResult DownloadTicket(int id)
        {
            var ticket = db.Tickets.FirstOrDefault(t => t.TicketId == id && t.UserEmail == User.Identity.Name && t.IsPaid);
            if (ticket == null)
                throw new HttpException(404, "Ticket not found.");

            var eventBooking = db.EventBookings
                                 .Include("EventQuote.Venue")
                                 .FirstOrDefault(e => e.EventBookingId == ticket.EventBookingId);

            string eventTitle = eventBooking?.EventQuote?.Title ?? "Private Event";
            DateTime? eventDate = eventBooking?.EventQuote?.EventStartDateTime;
            string venue = eventBooking?.EventQuote?.Venue?.Name ?? "Durban Hotel";

            var pdf = PdfHelper.GenerateTicketPDF(
                customerName: ticket.UserEmail,
                email: ticket.UserEmail,
                ticketType: ticket.TicketType,
                quantity: ticket.Quantity ?? 1,
                amount: ticket.AmountPaid,
                code : ticket.TicketCode,
                eventTitle: eventTitle,
                eventDate: eventDate,
                venue: venue
            );

            return File(pdf, "application/pdf", $"Ticket_{ticket.TicketId}.pdf");
        }

    }

}