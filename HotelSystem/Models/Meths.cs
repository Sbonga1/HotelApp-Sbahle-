using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    public class Meths
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
       
        private static readonly Random random = new Random();

        public static int GenerateRandomCode()
        {
             
            
            // Generate a random 4-digit code
            int code = random.Next(1000, 10000);
           
            return code;
        }
    }
}