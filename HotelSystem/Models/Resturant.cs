using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class Resturant
    {
        public int Id { get; set; }
        [Display(Name = "Resturant Name")]
        [Required(ErrorMessage = "Resturant name is required")]
        [MaxLength(45, ErrorMessage = "The Resturant name can be maximum 45 characters long")]
        public string Name { get; set; }
        public string Icon { get; set; }

    }
}