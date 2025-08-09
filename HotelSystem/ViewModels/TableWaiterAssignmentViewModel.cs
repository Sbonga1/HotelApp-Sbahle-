using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.ViewModels
{
    public class TableWaiterAssignmentViewModel
    {
        public int BookingId { get; set; }
        public List<TableAssignmentItem> Assignments { get; set; }
        public SelectList WaiterOptions { get; set; }
    }

    public class TableAssignmentItem
    {
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int SeatCount { get; set; }
        public string AssignedWaiterEmail { get; set; }
        public string SelectedWaiterEmail { get; set; }
    }
}

