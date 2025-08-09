using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Models
{
    [Bind(Exclude = "Id")]
    public class CustomerOrder
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [DisplayName("First Name")]

        public string FirstName { get; set; }
        [DisplayName("Last Name")]

        public string LastName { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        [DisplayName("Province")]
        public string State { get; set; }

        [DisplayName("Postal Code")]

        public string PostalCode { get; set; }


        public string Country { get; set; }


        public string Phone { get; set; }

        [DisplayName("Email Address")]

        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}",
            ErrorMessage = "Email is is not valid.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [ScaffoldColumn(false)]
        [Column(TypeName = "datetime2")]
        public DateTime DateCreated { get; set; }

        [ScaffoldColumn(false)]
        public Decimal Amount { get; set; }

        [ScaffoldColumn(false)]
        public string CustomerUserName { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime deliveryDate { get; set; }
        public string PreferredTime { get; set; }
        public string ShippingMethod { get; set; }
        public string Status { get; set; }
        public double deliveryFee { get; set; }
        //
        public int UniqueCode { get; set; }
        public int RoomNumber { get; set; }
        public int TableNumber { get; set; }

        public string qrCodePicture { get; set; }
        public bool IsDeliveryRescheduled { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime DeliveredOn { get; set; }
        public int DeliveredBy { get; set; }
        public string DeliveryType { get; set; }

        public bool IsReturnRescheduled { get; set; }
        public string DriverEmail { get; set; }
    }

}