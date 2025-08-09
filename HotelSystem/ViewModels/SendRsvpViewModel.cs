using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class SendRsvpViewModel
    {
        public int EventBookingId { get; set; }

        public string EventTitle { get; set; }

        [Required]
        [Display(Name = "Guest Emails (comma or newline separated)")]
        public string Emails { get; set; }
    }

}