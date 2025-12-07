using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IRepository<Admin> _adminRepository;
        // Sayıları çekmek için bu ikisini ekledik
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Product> _productRepository;

        // Constructor'da (Yapıcı Metot) hepsini istiyoruz
        public AdminController(IRepository<Admin> adminRepository, IRepository<Category> categoryRepository, IRepository<Product> productRepository)
        {
            _adminRepository = adminRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        public IActionResult Index()
        {
            // Sayıları veritabanından çekip ViewBag kutusuna koyuyoruz
            ViewBag.KategoriSayisi = _categoryRepository.GetAll().Count();
            ViewBag.UrunSayisi = _productRepository.GetAll().Count();

            return View();
        }

        // --- PROFİL KISIMLARI (DOKUNMA AYNI KALSIN) ---
        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var adminEmail = User.FindFirstValue(ClaimTypes.Name);
            var admin = _adminRepository.GetAll().FirstOrDefault(x => x.Email == adminEmail);

            if (admin == null) return NotFound("Yönetici bulunamadı.");

            if (admin.Password != model.CurrentPassword)
            {
                ModelState.AddModelError("", "Mevcut şifreniz hatalı.");
                return View(model);
            }

            admin.Password = model.NewPassword;
            _adminRepository.Update(admin);
            _adminRepository.Save();

            TempData["success"] = "Şifreniz başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }
    }
}