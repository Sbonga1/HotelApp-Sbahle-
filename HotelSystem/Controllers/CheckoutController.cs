using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using HotelSystem.Models;
using HotelSystem.Models;

namespace HotelSystem.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController()
        {
            _context = new ApplicationDbContext();
        }

        private string GenerateVoucherCode()
        {
            return Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }

        public ActionResult ApplyDiscount(int customerId, List<int> productIds, List<int> tableCategoryIds, decimal totalAmount)
        {
            var discounts = _context.Discounts
                                    .Where(d => d.IsActive && d.StartDate <= DateTime.Now && d.EndDate >= DateTime.Now)
                                    .ToList();

            bool discountApplied = false;
            Discount appliedDiscount = null;

            foreach (var discount in discounts)
            {
                if ((double)totalAmount >= discount.MinimumSpend)
                {
                    bool isApplicable = false;

                    if (discount.ApplicableItemType == "Product")
                    {
                        var applicableProductIds = discount.ApplicableItemIds.Split(',').Select(int.Parse).ToList();
                        isApplicable = productIds.Any(productId => applicableProductIds.Contains(productId));
                    }
                    else if (discount.ApplicableItemType == "TableCategory")
                    {
                        var applicableTableCategoryIds = discount.ApplicableItemIds.Split(',').Select(int.Parse).ToList();
                        isApplicable = tableCategoryIds.Any(tableCategoryId => applicableTableCategoryIds.Contains(tableCategoryId));
                    }

                    if (isApplicable)
                    {
                        var discountAmount = (double)totalAmount * (discount.Percentage / 100);
                        totalAmount -= (decimal)discountAmount;
                        appliedDiscount = discount;
                        discountApplied = true;
                        break;  // Assuming only one discount can be applied
                    }
                }
            }

            if (discountApplied && appliedDiscount != null)
            {
                var voucherCode = GenerateVoucherCode();

                var voucher = new Voucher
                {
                    Code = voucherCode,
                    DiscountId = appliedDiscount.Id,
                    CustomerId = customerId,
                    IsRedeemed = false,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMonths(1) // Optional expiry
                };

                _context.Vouchers.Add(voucher);
                _context.SaveChanges();

                ViewBag.VoucherCode = voucherCode;
            }

            return View("CheckoutSummary", new CheckoutSummaryViewModel
            {
                TotalAmount = totalAmount,
                AppliedDiscounts = discounts,
                VoucherCode = ViewBag.VoucherCode
            });
        }


        
    }
}
