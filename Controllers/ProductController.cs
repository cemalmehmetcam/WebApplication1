using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Include işlemleri için gerekli
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Controllers
{
    // [Authorize] // Gerekirse sınıf seviyesindeki bu yetkiyi kaldırıp metodlara özel verebilirsin. 
    // Şimdilik Müşterilerin de görebilmesi için Index ve Details'ı serbest bırakacağız.
    public class ProductController : Controller
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager; // EKLENDİ
        private readonly ApplicationDbContext _context; // EKLENDİ

        public ProductController(
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            UserManager<IdentityUser> userManager, // EKLENDİ
            ApplicationDbContext context) // EKLENDİ
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; // EKLENDİ
            _context = context; // EKLENDİ
        }

        // Listeleme (Herkese Açık Olmalı)
        public IActionResult Index(int? categoryId)
        {
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name", categoryId);
            IEnumerable<Product> productList;

            if (categoryId != null && categoryId != 0)
            {
                productList = _productRepository.GetAll(u => u.CategoryId == categoryId, includeProps: "Category");
            }
            else
            {
                productList = _productRepository.GetAll(includeProps: "Category");
            }

            return View(productList);
        }

        // --- YENİ EKLENEN: ÜRÜN DETAY SAYFASI ---
        public async Task<IActionResult> Details(int id)
        {
            // Repository yerine _context kullanarak karmaşık sorgu (Include User) yapıyoruz
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Ürüne ait yorumları getir
            ViewBag.Reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(product);
        }

        // --- YENİ EKLENEN: YORUM YAPMA ---
        [HttpPost]
        [Authorize] // Sadece üyeler yorum yapabilir
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                CreatedDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = productId });
        }

        // --- YÖNETİCİ İŞLEMLERİ (Create, Edit, Delete) ---

        [Authorize(Roles = "Admin")] // Sadece Admin görebilir
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(Product product, IFormFile? file)
        {
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    product.ImageUrl = @"/images/products/" + fileName;
                }

                _productRepository.Add(product);
                _productRepository.Save();
                TempData["success"] = "Ürün başarıyla eklendi.";
                return RedirectToAction("Index");
            }
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id == 0) return NotFound();

            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(Product product, IFormFile? file)
        {
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\', '/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    product.ImageUrl = @"/images/products/" + fileName;
                }
                _productRepository.Update(product);
                _productRepository.Save();
                TempData["success"] = "Ürün başarıyla güncellendi.";
                return RedirectToAction("Index");
            }
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public IActionResult DeleteAjax(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\', '/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            _productRepository.Delete(id);
            _productRepository.Save();

            return Json(new { success = true, message = "Ürün başarıyla silindi." });
        }
    }
}