using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // SignalR kütüphanesi eklendi
using Microsoft.EntityFrameworkCore;
using WebApplication1.Hubs; // Hub sınıfımızın olduğu yer
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize] // Sadece giriş yapmış üyeler sipariş verebilir
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<OrderHub> _hubContext; // SignalR HubContext tanımı

        // Constructor'a IHubContext eklendi
        public OrderController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // Kullanıcının Geçmiş Siparişleri
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Sipariş Tamamlama (Checkout) Ekranı
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);

            // Sepetteki ürünleri getir
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Message"] = "Sepetiniz boş, sipariş verilemez.";
                return RedirectToAction("Index", "Cart");
            }

            return View(cartItems);
        }

        // Siparişi Onayla ve Kaydet
        [HttpPost]
        public async Task<IActionResult> Checkout(string address) // Adres bilgisini formdan alacağız
        {
            var user = await _userManager.GetUserAsync(User);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

            // 1. Yeni Sipariş Oluştur
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                OrderStatus = "Hazırlanıyor",
                TotalAmount = cartItems.Sum(x => x.Quantity * x.Product.Price),
                OrderItems = new List<OrderItem>()
            };

            // 2. Sepet Detaylarını Sipariş Detaylarına Dönüştür
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                order.OrderItems.Add(orderItem);
            }

            _context.Orders.Add(order);

            // 3. Sepeti Temizle
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            // --- SIGNALR BİLDİRİMİ BURADA ---
            // Sipariş veritabanına işlendikten sonra adminlere bildirim gönderiyoruz.
            await _hubContext.Clients.All.SendAsync("ReceiveOrderNotification", $"Yeni bir sipariş alındı! Tutar: {order.TotalAmount:C}");
            // --------------------------------

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}