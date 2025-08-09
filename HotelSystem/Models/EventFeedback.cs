using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class EventFeedback
    {
        public int EventFeedbackId { get; set; }
        public int EventBookingId { get; set; }
        public string Email { get; set; }
        public string TicketCode { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime SubmittedOn { get; set; }
    }

}