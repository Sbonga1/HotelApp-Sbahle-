using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class CustomerWalletViewModel
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public double AccountBalance { get; set; }
        public List<WalletTransaction> Transactions { get; set; }
    }

}