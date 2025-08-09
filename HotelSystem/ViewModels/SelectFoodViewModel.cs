using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class SelectFoodViewModel
    {
        public int EventBookingId { get; set; }
        public string GuestEmail { get; set; }

        public List<Product> AvailableFoods { get; set; }

        public int SelectedFoodId { get; set; } // Only 1 dish allowed
    }

}