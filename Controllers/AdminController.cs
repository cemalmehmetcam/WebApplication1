using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Adminler girebilir
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Tüm Siparişleri Listele
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate) // En yeniden eskiye
                .ToListAsync();

            return View(orders);
        }

        // Sipariş Durumunu Güncelle (Ajax ile çalışacak)
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string orderStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.OrderStatus = orderStatus;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Sipariş durumu güncellendi." });
            }
            return Json(new { success = false, message = "Sipariş bulunamadı." });
        }
    }
}