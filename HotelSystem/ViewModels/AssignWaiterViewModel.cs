using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class AssignWaiterViewModel
    {
        public int OrderId { get; set; }
        public int WaiterId { get; set; }
        public string AssignmentType { get; set; }
        public int? TableNumber { get; set; }

        public List<Waiter> Waiters { get; set; } // List to populate dropdown
    }

}