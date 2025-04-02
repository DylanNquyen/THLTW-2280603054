﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NguyenNgocThien_BanHang.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NguyenNgocThien_BanHang.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) ||
                                              p.Description.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");

            return View(await products.ToListAsync());
        }

        // GET: Products/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock,CategoryId")] Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload hình ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "wwwroot", "images");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file duy nhất
                    var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    var extension = Path.GetExtension(imageFile.FileName);
                    var uniqueFileName = $"{fileName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{extension}";

                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Lưu đường dẫn tương đối vào database
                    product.ImageUrl = $"/images/{uniqueFileName}";
                }
                else
                {
                    // Sử dụng hình ảnh mặc định nếu không có file upload
                    product.ImageUrl = "/images/default-product.jpg";
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,CategoryId,ImageUrl")] Product product, IFormFile imageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh mới nếu có
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "wwwroot", "images");

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Xóa hình ảnh cũ nếu không phải hình mặc định
                        if (!string.IsNullOrEmpty(product.ImageUrl) &&
                            !product.ImageUrl.EndsWith("default-product.jpg") &&
                            System.IO.File.Exists(Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'))))
                        {
                            System.IO.File.Delete(Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/')));
                        }

                        // Tạo tên file duy nhất
                        var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                        var extension = Path.GetExtension(imageFile.FileName);
                        var uniqueFileName = $"{fileName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{extension}";

                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // Lưu đường dẫn tương đối vào database
                        product.ImageUrl = $"/images/{uniqueFileName}";
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            // Xóa hình ảnh nếu không phải hình mặc định
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl) &&
                !product.ImageUrl.EndsWith("default-product.jpg") &&
                System.IO.File.Exists(Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'))))
            {
                System.IO.File.Delete(Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/')));
            }

            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}