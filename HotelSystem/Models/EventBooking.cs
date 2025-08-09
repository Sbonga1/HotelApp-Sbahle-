using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public enum EventType
    {
        Private,
        Public
    }

    public class Venue
    {
        public int VenueId { get; set; }

        [Required]
        [Display(Name = "Venue Name")]
        public string Name { get; set; }

        [Required]
        public int Capacity { get; set; }
        [Display(Name = "Base Rate Per Hour")]
        [DataType(DataType.Currency)]
        public decimal BaseRatePerHour { get; set; }  

        [Required]
        [Display(Name = "Base Price")]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        public string ImageUrl { get; set; }
    }

    public class Activity
    {
        public int ActivityId { get; set; }

        [Required]
        [Display(Name = "Activity Name")]
        public string Name { get; set; }
        
        [Required]
        [Display(Name = "Price Per Guest")]
        [DataType(DataType.Currency)]
        public decimal PricePerGuest { get; set; }

        public string IconImageUrl { get; set; }
    }

    public class Equipment
    {
        public int EquipmentId { get; set; }

        [Required]
        [Display(Name = "Equipment Name")]
        public string Name { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Available Quantity")]
        public int QuantityAvailable { get; set; }
        [Display(Name = "Hourly Rate")]
        [DataType(DataType.Currency)]
        public decimal PricePerHour { get; set; }
        [Display(Name = "Price Per Unit")]
        [DataType(DataType.Currency)]
        public decimal PricePerUnit { get; set; }

        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }


    public class EventQuote
    {
        public int EventQuoteId { get; set; }

        public string UserEmail { get; set; }
        [Required]
        [Display(Name = "Event Type")]
        public EventType EventType { get; set; }
        public bool IsPrivate => EventType == EventType.Private;

        public string Status { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime EventStartDateTime { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EventEndDateTime { get; set; }

        [NotMapped]
        public double DurationHours => (EventEndDateTime - EventStartDateTime).TotalHours;

        public int GuestCount { get; set; }

        public int VenueId { get; set; }
        public virtual Venue Venue { get; set; }

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        public bool IsFinalized { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? FinalizedAt { get; set; } // Optional auditing

        [Display(Name = "Admin Notes")]
        public string AdminNotes { get; set; }

        public string SummaryHtml { get; set; }

        public virtual ICollection<Activity> SelectedActivities { get; set; }
        public virtual ICollection<Product> SelectedFoods { get; set; }
        public virtual ICollection<Equipment> SelectedEquipments { get; set; }
        public virtual ICollection<EventQuoteItem> QuoteItems { get; set; }
    }
    public class EventQuoteItem
    {
        public int EventQuoteItemId { get; set; }

        public string ItemType { get; set; }  // "Activity", "Product", "Equipment"
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        public int EventQuoteId { get; set; }
        public virtual EventQuote EventQuote { get; set; }
    }

    public class EventBooking
    {
        [Key]
        public int EventBookingId { get; set; }

        [Required]
        public int EventQuoteId { get; set; }
        public virtual EventQuote EventQuote { get; set; }

        [Required]
        [Display(Name = "User Email")]
        public string UserEmail { get; set; }
        

        [Display(Name = "Guest List / Notes")]
        public string GuestPreferences { get; set; }

       
        [Display(Name = "Booking Status")]
        public string Status { get; set; } = "Confirmed – Awaiting Payment"; // Or "Confirmed – Paid"

        public DateTime ConfirmedAt { get; set; } = DateTime.Now;

        [Display(Name = "Payment Completed")]
        public bool IsPaid { get; set; } = false;

        [Display(Name = "Total Paid")]
        public decimal AmountPaid { get; set; } = 0;
        [Display(Name = "Deposit Required")]

        public decimal DepositRequired { get; set; } 
        public decimal FinalTotalCost { get; set; } 
        public decimal FoodCost { get; set; } 

        public string PaymentReference { get; set; }
        public virtual ICollection<TicketType> TicketTypes { get; set; }
    }
}