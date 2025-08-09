using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class WaiterEventTableGroup
    {
        public EventBooking EventBooking { get; set; }
        public List<EventTable> Tables { get; set; }
    }

}