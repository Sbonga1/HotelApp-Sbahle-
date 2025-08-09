using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelSystem.Models
{
    public class TicketType
    {
        [Key]
        public int TicketTypeId { get; set; }

        [Required]
        public string Name { get; set; }  // E.g., Standard, VIP
        [Required]
        [Display(Name = "Available Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public string Description { get; set; }

        // FK to EventBooking for public events
        [Required]
        public int EventBookingId { get; set; }
        [NotMapped]
        public int SoldQuantity { get; set; }

        [ForeignKey("EventBookingId")]
        public virtual EventBooking Event { get; set; }
    }

    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [EmailAddress, Required]
        public string UserEmail { get; set; }

        [Required]
        public string TicketCode { get; set; }

        // Public Events
        public int? EventId { get; set; }
        public string TicketType { get; set; } // e.g. VIP, Standard
        public int? Quantity { get; set; }

        // Private Events
        public int? EventBookingId { get; set; }

        // Shared Fields
        [DataType(DataType.Currency)]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; } = false;
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; } 

        public string StripeReference { get; set; }
        public string QRCodeImagePath { get; set; }
        public string PdfTicketPath { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public DateTime IssuedAt { get; set; } = DateTime.Now;
    }

}
