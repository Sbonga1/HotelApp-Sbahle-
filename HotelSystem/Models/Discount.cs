using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelSystem.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Discount name is required.")]
        [StringLength(100, ErrorMessage = "Discount name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
        public double Percentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum spend must be a positive number.")]
        public double MinimumSpend { get; set; }
        [DataType("datetime2")]
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }
        [Column(TypeName = "datetime2")]
        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; }

        // New properties for applicable item types and IDs
        [Required(ErrorMessage = "Applicable item type is required.")]
        public string ApplicableItemType { get; set; }  // Could be "Product" or "TableCategory"

        public string ApplicableItemIds { get; set; }  // Comma-separated list of IDs

        [NotMapped]
        public bool IsExpired => EndDate < DateTime.Now;
    }
}
