using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class TableCategory
    {
        public int Id { get; set; }

        [Display(Name = "Category Name")]
        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50, ErrorMessage = "The category name can be a maximum of 50 characters long")]
        public string Name { get; set; }

        [Display(Name = "Maximum Guests")]
        [Range(1, 20, ErrorMessage = "Maximum guests must be between 1 and 20")]
        public int MaxGuests { get; set; }
        public int ResturantId { get; set; }
        public Resturant Resturant { get; set; }
        public string Icon { get; set; }
        [Display(Name = "Status")]
        public string Status { get; set; }
        public double Price { get; set; }
        public ICollection<TableReservation> TableReservations { get; set; }

    }
}