using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class RSVPViewModel
    {
        [Required]
        public int EventBookingId { get; set; } // Required for database lookup

        [Required]
        [EmailAddress]
        public string Email { get; set; } // Needed to validate against the invite

        public string RSVPToken { get; set; }

        public string EventTitle { get; set; }

        public DateTime EventDate { get; set; }

        public string VenueName { get; set; }

        public string Description { get; set; }

        [Required]
        public string Response { get; set; }

        public string Message { get; set; }
    }
}