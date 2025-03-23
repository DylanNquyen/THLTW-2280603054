//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using NguyenNgocThien_BanHang.Data;
//using NguyenNgocThien_BanHang.Models;
////using NguyenNgocThien_Buoi4.Data;
//using NguyenNgocThien_Buoi4.Models;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace NguyenNgocThien_Buoi4.Controllers
//{
//    public class HomeController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public HomeController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var featuredProducts = await _context.Products
//                .Include(p => p.Category)
//                .OrderByDescending(p => p.Id)
//                .Take(8)
//                .ToListAsync();

//            var categories = await _context.Categories.ToListAsync();

//            ViewBag.Categories = categories;

//            return View(featuredProducts);
//        }

//        public IActionResult Privacy()
//        {
//            return View();
//        }

//        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        public IActionResult Error()
//        {
//            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//        }
//    }
//}

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
////using NguyenNgocThien_Buoi4.Data;
//using NguyenNgocThien_Buoi4.Models;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Identity;
//using NguyenNgocThien_BanHang.Data;
//using NguyenNgocThien_BanHang.Models;

//namespace NguyenNgocThien_Buoi4.Controllers
//{
//    public class HomeController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public HomeController(
//            ApplicationDbContext context,
//            UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var featuredProducts = await _context.Products
//                .Include(p => p.Category)
//                .OrderByDescending(p => p.Id)
//                .Take(8)
//                .ToListAsync();

//            var categories = await _context.Categories.ToListAsync();

//            ViewBag.Categories = categories;

//            // Kiểm tra nếu người dùng đã đăng nhập và có quyền Admin
//            if (User.Identity.IsAuthenticated)
//            {
//                var user = await _userManager.GetUserAsync(User);
//                ViewBag.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
//                ViewBag.IsEmployee = await _userManager.IsInRoleAsync(user, "Employee");
//            }

//            return View(featuredProducts);
//        }

//        public IActionResult Privacy()
//        {
//            return View();
//        }

//        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        public IActionResult Error()
//        {
//            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenNgocThien_BanHang.Data;
using NguyenNgocThien_BanHang.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace NguyenNgocThien_BanHang.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Kiểm tra vai trò người dùng
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.IsEmployee = User.IsInRole("Employee");

            // Lấy danh sách sản phẩm nổi bật (8 sản phẩm mới nhất)
            var featuredProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            // Lấy danh sách danh mục
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(featuredProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}