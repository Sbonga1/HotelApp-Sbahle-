using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.Models
{
    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerOrderId { get; set; } // kept only as a scalar key

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Complaint description is required.")]
        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(1000)]
        public string AdminResponse { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // ❌ Removed CustomerOrder navigation to break cascade cycle
        // public virtual CustomerOrder CustomerOrder { get; set; }

        public virtual ICollection<ComplaintItem> ComplaintItems { get; set; } = new List<ComplaintItem>();
    }


    public class ComplaintItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }

        [Required]
        public int OrderedProductId { get; set; }

        public virtual Complaint Complaint { get; set; }

        public virtual OrderedProduct OrderedProduct { get; set; }
    }
}
