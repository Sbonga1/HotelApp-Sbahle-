using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelSystem.Models
{
    public class Refund
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RefundId { get; set; }
       
        public string ResevationDate { get; set; }
        public int ReservationId { get; set; }
        public double reservationAmtPaid { get; set; }
        public string Reason { get; set; }
      
        public string RefundDate { get; set; }
        public string RefundStatus { get; set; }
        public string emailaddress { get; set; }
        public double RefundFee { get; set; }
        public double tobePaid { get; set; }
        public string signature { get; set; }
        
       
    }
}
