using System;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.ViewModels
{
    public class EnterBuyerInfoViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string BuyerName { get; set; }


        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string BuyerEmail { get; set; }

        // Holds TempData ticket info
        public int EventBookingId { get; set; }
        public int SelectedTicketTypeId { get; set; }
        public string TicketTypeName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerTicket { get; set; }
        public decimal TotalAmount => Quantity * PricePerTicket;
    }
}