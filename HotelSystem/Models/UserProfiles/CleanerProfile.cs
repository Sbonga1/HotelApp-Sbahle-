using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Models.UserProfiles
{
    public enum SpecialityEnum
    {
        Cleaner,
        Plumber,
        Electrician,
        HVAC_Technician,
        Inspector
       
    }
    public class CleanerProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public string Specialty { get; set; }
        public IEnumerable<SelectListItem> Specialties { get; set; }
        public bool Availability { get; set; }
        public string picture { get; set; }
        //[DisplayName("Hotel Name:")]
        //public string Hotel { get; set; }
    }
}