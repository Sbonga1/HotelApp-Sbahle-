using HotelSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.ViewModels
{
    public class SubmitComplaintViewModel
    {
        [Required]
        public int CustomerOrderId { get; set; }

        [Required(ErrorMessage = "Please provide a description of your complaint.")]
        [Display(Name = "Complaint Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public List<OrderedProduct> OrderedProducts { get; set; }

       
        [Required(ErrorMessage = "Please select at least one product to complain about.")]
        [Display(Name = "Products You Are Complaining About")]
        public List<int> SelectedOrderedProductIds { get; set; }

        
        public string ComplaintMessage { get; set; }
    }
}
