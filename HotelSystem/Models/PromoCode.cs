using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelSystem.Models
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Promo code is required.")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Discount amount is required.")]
        public double Amount { get; set; }

        public bool IsRedeemed { get; set; }

        [Column(TypeName = "datetime2")]
        [Required(ErrorMessage = "Expiry date is required.")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public string UserId { get; set; }  // Assuming ASP.NET Identity is used
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }
    }
}
