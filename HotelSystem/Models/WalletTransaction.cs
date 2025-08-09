using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserEmail { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public string Type { get; set; } 

        public string Description { get; set; }

        public double Amount { get; set; }

        public double BalanceAfterTransaction { get; set; }
    }

}