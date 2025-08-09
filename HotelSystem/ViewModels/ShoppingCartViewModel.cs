using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class ShoppingCartViewModel
    {
        public List<Cart> CartItems { get; set; }
        public double CartTotal { get; set; }
        public double DiscountAmount { get; set; }
        public double FinalTotal { get; set; }
        public string PromoCode { get; set; }
        public double AccountBalance { get; set; }

        public Discount Discount { get; set; }

    }
}