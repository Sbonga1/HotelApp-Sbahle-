using System;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.Models
{
    public class TableReservation
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string qrCodePicture { get; set; }
        public string CancellationReason { get; set; }
        public DateTime ReservationDate { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string SpecialRequests { get; set; }
        public string Status { get; set; }
        public int UniqueCode { get; set; }
        public int TableCategoryId { get; set; }
        public TableCategory TableCategory { get; set; }
        public void ValidateNumberOfGuests()
        {
            if (NumberOfGuests > TableCategory.MaxGuests)
            {
                throw new ValidationException($"The selected table can accommodate a maximum of {TableCategory.MaxGuests} guests.");
            }
        }
    }

    
}
