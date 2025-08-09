using Microsoft.AspNet.Identity;
using HotelSystem.Models;
using HotelSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace HotelSystem.Controllers
{
    public class ShoppingCartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Promocodes()
        {
            var codes = db.PromoCodes.Where(x => x.UserId == User.Identity.Name && x.ExpiryDate >= DateTime.Now).ToList();
            return View(codes);
        }
        public ActionResult Index()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);
            var account = db.CustInfos.FirstOrDefault(x => x.Email == User.Identity.Name);
            double balance = account?.AccountBalance ?? 0;

            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cart.GetCartItems(),
                CartTotal = (double)cart.GetTotal(),
                AccountBalance = balance
            };

            return View(viewModel);
        }

        public ActionResult AddToCart(int id)
        {
            var addedProduct = db.Products.Single(product => product.Id == id);

            var cart = ShoppingCart.GetCart(this.HttpContext);

            cart.AddToCart(addedProduct);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateCart(int productId, int newQuantity)
        {
            var cart = ShoppingCart.GetCart(HttpContext);
            cart.UpdateCartItemQuantity(productId, newQuantity);

            decimal cartTotal = cart.GetTotal();

            return Json(new { cartTotal = cartTotal, message = "Cart updated successfully." });
        }

        public ActionResult RemoveFromCart(int id)
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            string productName = db.Carts.FirstOrDefault(item => item.ProductId == id).Product.Name;

            int itemCount = cart.RemoveFromCart(id);

            var results = new ShoppingCartRemoveViewModel
            {
                Message = Server.HtmlEncode(productName) + " has been removed from your shopping cart",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            return RedirectToAction("Index");
        }

        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            ViewData["CartCount"] = cart.GetCount();
            return PartialView("CartSummary");
        }
        public ActionResult Checkout(string promoCode, string deliveryOption, string preferredTime, bool useBalance = false)
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);
            var userEmail = User.Identity.Name;
            var customer = db.CustInfos.FirstOrDefault(c => c.Email == userEmail);
            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            var cartTotal = cart.GetTotal();

            var discount = db.Discounts.FirstOrDefault(d =>
                d.IsActive &&
                d.MinimumSpend <= (double)cartTotal &&
                d.StartDate <= DateTime.Now &&
                d.EndDate >= DateTime.Now &&
                d.ApplicableItemType == "Product"
            );

            double discountAmount = discount != null ? (double)cartTotal * (discount.Percentage / 100.0) : 0;
            double finalTotal = (double)cartTotal - discountAmount;

            if (!string.IsNullOrEmpty(promoCode))
            {
                var promo = db.PromoCodes.FirstOrDefault(p => p.Code == promoCode && !p.IsRedeemed && p.ExpiryDate >= DateTime.Now);
                if (promo != null)
                {
                    finalTotal -= promo.Amount;
                    promo.IsRedeemed = true;
                    db.Entry(promo).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    TempData["Error"] = "Invalid or expired promo code.";
                    return RedirectToAction("Index");
                }
            }

            TempData["DeliveryOption"] = deliveryOption;
            customer.Delivery = deliveryOption;

            if (!string.IsNullOrEmpty(preferredTime) && TimeSpan.TryParse(preferredTime, out TimeSpan time))
            {
                customer.Time = DateTime.Today.Add(time); 
            }

            db.Entry(customer).State = EntityState.Modified;
            db.SaveChanges();

            if (useBalance)
            {
                double walletUsed = 0;
                if (finalTotal <= customer.AccountBalance)
                {
                    walletUsed = finalTotal;
                    TempData["WalletUsed"] = walletUsed;

                    GeneratePromoAfterCheckout(userEmail, cartTotal, discountAmount, discount);

                    return RedirectToAction("PaymentSuccess", "Payment", new { session_id = "WALLET" });
                }

                walletUsed = customer.AccountBalance;
                finalTotal -= walletUsed;
                TempData["WalletUsed"] = walletUsed;
            }

            GeneratePromoAfterCheckout(userEmail, cartTotal, discountAmount, discount);

            return RedirectToAction("CreatePayment", "Payment", new { total = finalTotal, deliveryOption });
        }

        private void GeneratePromoAfterCheckout(string email, decimal cartTotal, double discountAmount, Discount discount)
        {
            if (discount == null || discountAmount <= 0) return;

            var existingPromo = db.PromoCodes.FirstOrDefault(p =>
                p.UserId == email && !p.IsRedeemed && p.ExpiryDate >= DateTime.Now);

            if (existingPromo != null) return; 

            var promoCode = GeneratePromoCode();
            db.PromoCodes.Add(new PromoCode
            {
                Code = promoCode,
                Amount = Math.Round(discountAmount, 2),
                ExpiryDate = DateTime.Now.AddMonths(1),
                IsRedeemed = false,
                UserId = email
            });

            db.SaveChanges();
            TempData["NewPromo"] = $"🎁 You received a promo code: {promoCode} worth R{discountAmount:F2}!";
        }
        private string GeneratePromoCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        
    }
}
