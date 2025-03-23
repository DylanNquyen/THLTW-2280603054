using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenNgocThien_BanHang.Data;
//using NguyenNgocThien_Buoi4.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NguyenNgocThien_Buoi4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng quan
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Đơn hàng
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending");
            ViewBag.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "processing");
            ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "completed");
            ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "cancelled");

            // Doanh thu
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "completed")
                .SumAsync(o => o.TotalAmount);

            // Doanh thu theo tháng (6 tháng gần nhất)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyRevenue = await _context.Orders
                .Where(o => o.Status == "completed" && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { Month = o.OrderDate.Month, Year = o.OrderDate.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlyRevenue = monthlyRevenue;

            // Sản phẩm bán chạy
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            // Đơn hàng gần đây
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }
    }
}