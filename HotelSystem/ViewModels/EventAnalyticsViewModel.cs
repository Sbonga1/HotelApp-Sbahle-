using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.ViewModels
{
    public class PublicEventAnalyticsViewModel
    {
        public string EventTitle { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TicketsScanned { get; set; }
        public int TicketsPending { get; set; }

        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public Dictionary<int, int> RatingBreakdown { get; set; } // key: star, value: count
    }

    public class PrivateEventAnalyticsViewModel
    {
        public string EventTitle { get; set; }
        public int InvitesSent { get; set; }
        public int ResponsesYes { get; set; }
        public int ResponsesNo { get; set; }
        public int NoResponse { get; set; }
        public int Attended { get; set; }
    }

}