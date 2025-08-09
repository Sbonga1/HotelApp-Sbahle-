using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class Reservation
    {
       
        public int Id { get; set; }
        public string CustomerUsername { get; set; }
        [Required(ErrorMessage = "Required!")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        //
        [Required(ErrorMessage = "Required!")]
        [RegularExpression("[A-Za-z]*", ErrorMessage = "Use letters only please")]
        [StringLength(45, MinimumLength = 3, ErrorMessage = "Please enter a valid name")]
        public string CustomerName { get; set; }
        //
        [Required(ErrorMessage = "Required!")]
        [RegularExpression("[A-Za-z]*", ErrorMessage = "Use letters only please")]
        [StringLength(45, MinimumLength = 3, ErrorMessage = "Please enter a valid name")]
        public string CustomerSurname { get; set; }
        //
        [Required(ErrorMessage = "Required!")]
        [DataType(DataType.Date)]
        public DateTime From { get; set; }
        //
        [Required(ErrorMessage = "Required!")]
        [DataType(DataType.Time)]
        public DateTime Time { get; set; }
        //
        [Required(ErrorMessage = "Required!")]
        [MinLength(1, ErrorMessage = "1 digits")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Only number(s) allowed!")]
        public string Nights { get; set; }
        public double Cost { get; set; }
        public double AmtPaid { get; set; }
        public double PromoAmt { get; set; }
        public double WalletAmt { get; set; }
        public string RoomType { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public string CheckInCode { get; set; }
        public int ApartmentId { get; set; }
        public virtual Apartment Apartment { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }


        public string GetStatus()
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var status = (from c in db.room
                          where c.ID == ApartmentId
                          select c.ID).Single();

            var currentStatus = (from c in db.room
                                 where c.ID == status
                                 select c.Status).Single();

            return (currentStatus);
        }

        public string GetRate()
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var price = (from c in db.room
                         where c.ID == ApartmentId
                         select c.ID).Single();

            var rate = (from c in db.room
                        where c.ID == price
                        select c.rating).Single();

            return (rate).ToString();
        }

        public string GetRoom()
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var room = (from c in db.room
                        where c.ID == ApartmentId
                        select c.ID).Single();

            var type = (from c in db.room
                        where c.ID == room
                        select c.Room).Single();

            return (type);
        }
        public string GetRoomType()
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var room = (from c in db.room
                        where c.ID == ApartmentId
                        select c.ID).Single();

            var type = (from c in db.room
                        where c.ID == room
                        select c.RoomType).Single();

            return (type);
        }
    }
}