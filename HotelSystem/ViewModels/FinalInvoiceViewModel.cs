using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class FinalInvoiceViewModel
    {
        public EventBooking Booking { get; set; }

        public decimal VenueCost { get; set; }
        public decimal ActivityCost { get; set; }
        public decimal EstimatedFoodCost { get; set; }

        public Product ActualFood { get; set; }
        public decimal ActualFoodCost { get; set; }
        public Dictionary<string, FoodBreakdownInfo> FoodBreakdown { get; set; }


        public decimal FinalTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
    }

}