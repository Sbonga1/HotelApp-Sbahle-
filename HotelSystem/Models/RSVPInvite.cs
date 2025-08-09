using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelSystem.Models
{
    public class RSVPInvite
    {
        [Key]
        public int RSVPInviteId { get; set; }

        [Required]
        public int EventBookingId { get; set; }

        [ForeignKey("EventBookingId")]
        public virtual EventBooking EventBooking { get; set; }

        [Required]
        [EmailAddress]
        public string GuestEmail { get; set; }
        public string Response { get; set; }

        public bool HasResponded { get; set; } = false;

        public DateTime? ResponseDate { get; set; }

        public string RSVPToken { get; set; } // Optional: link to generated ticket
    }
}
