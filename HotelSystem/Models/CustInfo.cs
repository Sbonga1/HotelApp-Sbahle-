using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class CustInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime Date { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime Time { get; set; }
        public string Email { get; set; }
        public string Delivery { get; set; }
        public int ReservID { get; set; }
        public int TableID { get; set; }
        public double AccountBalance { get; set; }
    }
}