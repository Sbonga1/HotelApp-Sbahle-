using HotelSystem.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelSystem.Models
{
    public class DiscountFormViewModel
    {
        public Discount Discount { get; set; }

        [Required(ErrorMessage = "Please select a type")]
        public string SelectedItemType { get; set; }  // "Product" or "TableCategory"

        public List<int> SelectedProductIds { get; set; }
        public List<int> SelectedTableCategoryIds { get; set; }

        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<TableCategory> TableCategories { get; set; }
    }
}
