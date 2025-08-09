using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class CheckIN
    {
        
        public int ID { get; set; }
        
        public string CheckinDate { get; set; }
        
        public string CheckinTime { get; set; }
        
      
        public string Signature { get; set;}
        public string CustSignature { get; set; }
        public string Room { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
    }
}