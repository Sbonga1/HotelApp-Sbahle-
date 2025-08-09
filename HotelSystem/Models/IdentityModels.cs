using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows.Controls;
using HotelSystem.Models.UserProfiles;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Stripe;
using Stripe.Climate;

namespace HotelSystem.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
        public virtual DbSet<Reservation> Reservations { get; set; }
        public virtual DbSet<Refund> Refunds { get; set; }
        public virtual DbSet<Invoice> Invoices { get; set; }
        public virtual DbSet<UserDetail> UserDetails { get; set; }
        public virtual DbSet<CheckIN> CheckINs { get; set; }
        public virtual DbSet<Apartment> room { get; set; }
        public virtual DbSet<CustInfo> CustInfos { get; set; }
        public virtual DbSet<TableCategory> TableCategories { get; set; }
        public virtual DbSet<TableReservation> TableReservations { get; set; }
        public virtual DbSet<PromoCode> PromoCodes { get; set; }
        public virtual DbSet<CustomerOrder> CustomerOrders { get; set; }
        public virtual DbSet<OrderedProduct> Orderedproducts { get; set; }
        public virtual DbSet<Cart> Carts { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Voucher> Vouchers { get; set; }
        public virtual DbSet<Resturant> Resturants { get; set; }
        public virtual DbSet<Discount> Discounts { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }
        public virtual DbSet<Waiter> Waiters { get; set; }
        public virtual DbSet<WaiterAssignment> WaiterAssignments { get; set; }
        public virtual DbSet<Complaint> Complaints { get; set; }
        public virtual DbSet<ComplaintItem> ComplaintItems { get; set; }


        public virtual DbSet<Venue> Venues { get; set; }
        public virtual DbSet<Activity> Activities { get; set; }
        public virtual DbSet<Equipment> Equipment { get; set; }
        public virtual DbSet<EventQuote> Quotes { get; set; }
        public virtual DbSet<EventBooking> EventBookings { get; set; }
        public virtual DbSet<Ticket> Tickets { get; set; }
        public virtual DbSet<TicketType> TicketTypes { get; set; }
        public virtual DbSet<RSVPInvite> RSVPInvites { get; set; }
        public virtual DbSet<EventFeedback> EventFeedbacks { get; set; }
        public virtual DbSet<EventTable> EventTables { get; set; }
        public virtual DbSet<Seat> Seats { get; set; }
        public virtual DbSet<EventFoodSelection> EventFoodSelections { get; set; }

    }
}