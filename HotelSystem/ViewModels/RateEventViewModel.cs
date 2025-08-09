using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class RateEventViewModel
    {
        public int EventBookingId { get; set; }
        public string EventTitle { get; set; }

        [Required(ErrorMessage = "Ticket code is required.")]
        public string TicketCode { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }

}