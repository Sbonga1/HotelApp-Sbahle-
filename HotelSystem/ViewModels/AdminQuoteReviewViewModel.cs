using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class AdminQuoteReviewViewModel
    {
        public int QuoteId { get; set; }
        public string ClientName { get; set; }
        public string Title { get; set; }
        public string EventType { get; set; }
        public string ActionType { get; set; }
        public int GuestCount { get; set; }
        public double DurationHours { get; set; }
        public DateTime EventStartDateTime { get; set; }
        public DateTime EventEndDateTime { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsFinalized { get; set; }


        public Venue Venue { get; set; }
        public List<Activity> SelectedActivities { get; set; }
        public List<Product> SelectedFoods { get; set; }
        public List<Equipment> SelectedEquipments { get; set; }

        public decimal EstimatedTotal { get; set; }
        public string Status { get; set; } 
        public string AdminNotes { get; set; }
        public string ClientFullName { get; set; }
    }

}