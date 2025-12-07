using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment; // Resim kaydetmek için gerekli

        public ProductController(IRepository<Product> productRepository, IRepository<Category> categoryRepository, IWebHostEnvironment webHostEnvironment)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            // Kategorileri de dahil ederek (Include) ürünleri getiriyoruz
            var productList = _productRepository.GetAll("Category");
            return View(productList);
        }

        // --- EKLEME SAYFASI ---
        [HttpGet]
        public IActionResult Create()
        {
            // Dropdown (Açılır Kutu) için kategorileri View'a gönderiyoruz
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product, IFormFile? file)
        {
            // Kategori doğrulama hatasını siliyoruz (Navigation property hatası olmasın diye)
            ModelState.Remove("Category");

            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                // --- RESİM YÜKLEME İŞLEMİ ---
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    // Dosya adını benzersiz yap (Guid) + uzantısını al (.jpg, .png)
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    // Klasör yoksa oluştur
                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    // Resmi kaydet
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    // Veritabanına kaydedilecek yol
                    product.ImageUrl = @"/images/products/" + fileName;
                }
                // -----------------------------

                _productRepository.Add(product);
                _productRepository.Save();
                TempData["success"] = "Ürün başarıyla eklendi.";
                return RedirectToAction("Index");
            }

            // Hata varsa kategorileri tekrar doldurup sayfayı geri döndür
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        // --- DÜZENLEME SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id == 0) return NotFound();

            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            // Kategorileri dropdown için gönderiyoruz
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        // --- DÜZENLEME İŞLEMİ (POST) ---
        [HttpPost]
        public IActionResult Edit(Product product, IFormFile? file)
        {
            // Validasyon engellerini kaldır
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                // Eğer yeni bir resim yüklendiyse
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    // Eski resmi sil (Eğer varsa)
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\', '/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Yeni resmi kaydet
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    // Modeldeki resim yolunu güncelle
                    product.ImageUrl = @"/images/products/" + fileName;
                }

                // Eğer resim yüklenmediyse, View'dan gelen (gizli input'taki) eski ImageUrl korunur.

                _productRepository.Update(product);
                _productRepository.Save();
                TempData["success"] = "Ürün başarıyla güncellendi.";
                return RedirectToAction("Index");
            }

            // Hata varsa listeyi tekrar doldur
            ViewBag.CategoryList = new SelectList(_categoryRepository.GetAll(), "Id", "Name");
            return View(product);
        }

        [HttpDelete]
        public IActionResult DeleteAjax(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }

            // 1. Varsa Resmi Klasörden Sil
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\', '/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // 2. Veritabanından Sil
            _productRepository.Delete(id);
            _productRepository.Save();

            return Json(new { success = true, message = "Ürün başarıyla silindi." });
        }
    }
}