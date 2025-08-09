using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HotelSystem.Models.UserProfiles
{
    public class AssignCleaner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ResevId { get; set; }
        public string Email { get; set; }
        public string CleanerEmail { get; set; }
        public string CleanerName { get; set; }
        public string CustomerName { get; set; }
        public string Room { get; set; }
        [DataType(DataType.Date)]
        public string date { get; set; }
        [DataType(DataType.Time)]
        public string time { get; set; }
        public string status { get; set; }
        public int reqId { get; set; }
        public bool IsApproved { get; set; }
    }
}