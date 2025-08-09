using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HotelSystem.Models
{
    using System;
    using System.Web;

    public static class CookieHelper
    {
        // Method to set a cookie with an integer value
        public static void SetCookie(string key, int value, int? expirationMinutes = null)
        {
            // Create a cookie with the specified key and value
            HttpCookie cookie = new HttpCookie(key, value.ToString());

            // Set the expiration date if provided
            if (expirationMinutes.HasValue)
            {
                cookie.Expires = DateTime.Now.AddMinutes(expirationMinutes.Value);
            }

            // Add the cookie to the response
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        // Method to retrieve the cookie value and dismiss it (delete it) afterward
        public static int? GetAndDeleteCookie(string key)
        {
            // Retrieve the cookie from the request
            HttpCookie cookie = HttpContext.Current.Request.Cookies[key];

            if (cookie != null)
            {
                // Convert the cookie value back to an integer
                int value = int.Parse(cookie.Value);

                // Dismiss (delete) the cookie by setting its expiration date to a past date
                cookie.Expires = DateTime.Now.AddDays(-1);
                HttpContext.Current.Response.Cookies.Add(cookie);

                // Return the retrieved value
                return value;
            }

            // Return null if the cookie doesn't exist
            return null;
        }

        // Method to delete a cookie explicitly
        public static void DeleteCookie(string key)
        {
            HttpCookie cookie = new HttpCookie(key)
            {
                Expires = DateTime.Now.AddDays(-1)
            };

            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }

}