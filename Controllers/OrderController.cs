using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenNgocThien_BanHang.Data;
using NguyenNgocThien_BanHang.Services;
//using NguyenNgocThien_Buoi4.Data;
using NguyenNgocThien_Buoi4.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NguyenNgocThien_Buoi4.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cartService;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            CartService cartService)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string status)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isEmployee = await _userManager.IsInRoleAsync(user, "Employee");

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Nếu không phải admin hoặc nhân viên, chỉ hiển thị đơn hàng của người dùng hiện tại
            if (!isAdmin && !isEmployee)
            {
                orders = orders.Where(o => o.UserId == user.Id);
            }

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                orders = orders.Where(o => o.Status == status);
            }

            // Sắp xếp theo ngày đặt hàng mới nhất
            orders = orders.OrderByDescending(o => o.OrderDate);

            ViewBag.Status = status;
            return View(await orders.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isEmployee = await _userManager.IsInRoleAsync(user, "Employee");

            if (!isAdmin && !isEmployee && order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var cart = _cartService.GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Store");
            }

            var user = await _userManager.GetUserAsync(User);
            ViewBag.Cart = cart;
            ViewBag.Total = _cartService.GetTotal();
            ViewBag.User = user;

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([Bind("ShippingAddress,PhoneNumber")] Order order)
        {
            var cart = _cartService.GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Store");
            }

            var user = await _userManager.GetUserAsync(User);
            order.UserId = user.Id;
            order.OrderDate = DateTime.Now;
            order.Status = "pending";
            order.TotalAmount = _cartService.GetTotal();

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();

                // Thêm các mục đơn hàng
                foreach (var item in cart)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.OrderItems.Add(orderItem);

                    // Cập nhật số lượng sản phẩm
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        _context.Update(product);
                    }
                }

                await _context.SaveChangesAsync();

                // Xóa giỏ hàng
                _cartService.ClearCart();

                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            ViewBag.Cart = cart;
            ViewBag.Total = _cartService.GetTotal();
            ViewBag.User = user;

            return View(order);
        }

        // POST: Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // POST: Orders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isEmployee = await _userManager.IsInRoleAsync(user, "Employee");

            if (!isAdmin && !isEmployee && order.UserId != user.Id)
            {
                return Forbid();
            }

            // Chỉ cho phép hủy đơn hàng ở trạng thái pending hoặc processing
            if (order.Status != "pending" && order.Status != "processing")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng ở trạng thái này.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // Cập nhật trạng thái đơn hàng
            order.Status = "cancelled";
            _context.Update(order);

            // Hoàn trả số lượng sản phẩm
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    _context.Update(product);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}