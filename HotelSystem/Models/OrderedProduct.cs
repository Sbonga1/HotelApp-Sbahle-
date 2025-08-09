using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class OrderedProduct
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int CustomerOrderId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("CustomerOrderId")]
        public virtual CustomerOrder CustomerOrder { get; set; }
    }


}