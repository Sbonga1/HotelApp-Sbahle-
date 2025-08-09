using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.ViewModels
{
    public class BuyTicketViewModel
    {
        public int EventBookingId { get; set; }

        public string EventTitle { get; set; }

        public DateTime EventDate { get; set; }

        public string Venue { get; set; }

        [Required(ErrorMessage = "Please select a ticket type.")]
        [Display(Name = "Ticket Type")]
        public int SelectedTicketTypeId { get; set; }

        [Required(ErrorMessage = "Please specify quantity.")]
        [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        // Ticket options to display
        public List<TicketTypeWithStats> AvailableTicketTypes { get; set; } = new List<TicketTypeWithStats>();
    }

    public class TicketTypeWithStats
    {
        public int TicketTypeId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
        public int SoldQuantity { get; set; }
    }
}
