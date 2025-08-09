using HotelSystem.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        public int DiscountId { get; set; }
        public int CustomerId { get; set; }

        public bool IsRedeemed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public virtual Discount Discount { get; set; }
    }
}
