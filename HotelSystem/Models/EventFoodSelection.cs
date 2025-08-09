using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class EventFoodSelection
    {
        public int Id { get; set; }

        public int EventBookingId { get; set; }
        public int ProductId { get; set; }

        public string UserEmail { get; set; }
        public DateTime SelectedAt { get; set; }

        public virtual EventBooking EventBooking { get; set; }
        public virtual Product Product { get; set; }
    }

}