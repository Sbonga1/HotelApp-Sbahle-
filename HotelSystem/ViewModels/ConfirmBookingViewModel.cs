using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class ConfirmBookingViewModel
    {
        public int QuoteId { get; set; }

        public string EventType { get; set; }
        public string SummaryHtml { get; set; }
        public decimal TotalCost { get; set; }

        [Display(Name = "Additional Notes / Preferences")]
        public string Preferences { get; set; }

       
        public DateTime EventStartDateTime { get; set; }
        public DateTime EventEndDateTime { get; set; }
        public int GuestCount { get; set; }

        [Required]
        public bool AcceptTerms { get; set; }
    }

}