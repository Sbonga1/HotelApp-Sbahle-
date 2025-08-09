using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.ViewModels
{
    public class RequestQuoteViewModel
    {
        [Required]
        public int VenueId { get; set; }
        [Required]
        [Display(Name = "Event Type")]
        public EventType EventType { get; set; }

        public int EventQuoteId { get; set; }
        public DateTime EventStartDateTime { get; set; }
        public DateTime EventEndDateTime { get; set; }
        public string QuoteStatus { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsFinalized { get; set; }

       
        
        [Display(Name = "Duration (Hours)")]
        public int DurationHours { get; set; }

        [Required]
        [Range(1, 1000)]
        public int GuestCount { get; set; }

        public List<Venue> VenueDetails { get; set; }
        public List<int> SelectedActivityIds { get; set; }
        public List<int> SelectedFoodIds { get; set; }
        public List<int> SelectedEquipmentIds { get; set; }

        // Dropdowns / Lists
        public List<SelectListItem> Venues { get; set; }
        public List<Activity> Activities { get; set; }
        public List<Product> Foods { get; set; }
        public List<Equipment> Equipments { get; set; }

        public decimal EstimatedTotal { get; set; }
        public string SummaryHtml { get; set; }
    }

}