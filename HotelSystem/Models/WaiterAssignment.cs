using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class WaiterAssignment
    {
        public int Id { get; set; }
        public int CustomerOrderId { get; set; }
        public int WaiterId { get; set; }
        public string AssignmentType { get; set; } 
        public DateTime AssignedAt { get; set; }
        public virtual CustomerOrder CustomerOrder { get; set; }
        public virtual Waiter Waiter { get; set; }
    }

}