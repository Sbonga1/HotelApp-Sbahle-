using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    
    public class Apartment
    {
        public int ID { get; set; }
        //public int number { get; set; }
        public string Room { get; set; }
        public string RoomType { get; set; } 
        public string Capacity { get; set; }
        public int floor { get; set; }
        public string description { get; set; }
        [DisplayName("price")]
        public int rating { get; set; }
        public string image { get; set; }
        public string Status { get; set; }
        public bool booked { get; set; }
       
    }
}