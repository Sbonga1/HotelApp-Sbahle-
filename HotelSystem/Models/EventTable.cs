using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class EventTable
    {
        public int EventTableId { get; set; }

        [Required]
        public int VenueId { get; set; }

        [Required]
        [StringLength(50)]
        public string TableName { get; set; }
        public string ImageUrl { get; set; }
        public string AssignedWaiterEmail { get; set; }

        public virtual ICollection<Seat> Seats { get; set; }

        public virtual Venue Venue { get; set; }
    }


    public class Seat
    {
        public int SeatId { get; set; }

        [Required]
        public int EventTableId { get; set; }

        [Required]
        [StringLength(20)]
        public string SeatNumber { get; set; }

        public bool IsOccupied { get; set; }

        public string OccupiedByEmail { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? ServedAt { get; set; }

        public virtual EventTable EventTable { get; set; }
        public virtual ICollection<EventFoodSelection> EventFoodSelections { get; set; }
    }


}