using HotelSystem.Models;
using System.Collections.Generic;

namespace HotelSystem.Models
{
    public class CheckoutSummaryViewModel
    {
        public decimal TotalAmount { get; set; }
        public List<Discount> AppliedDiscounts { get; set; }
        public string VoucherCode { get; set; }
    }
}
