using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelSystem.Models.UserProfiles
{
    public enum RoleEnum
    {
        Cleaner,
        Technician,
        Inspector
    }
    public class User
    {
        public RoleEnum Role { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
    }
}