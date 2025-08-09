using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DisplayName("Invoice #")]
        public string InvoiceNumber { get; set; }
        public string Invoice_Date { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string Damage_Description { get; set; }
        public string CostBreakdown { get; set; }
        public double Total_Amount_Due { get; set; }
        public string Signature { get; set; }
        public string status { get; set; }
        public string resevId { get; set; }
        public string Room { get; set; }

    }
}