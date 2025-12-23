using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SupportController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // KULLANICI: Kendi taleplerini görsün
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return View(tickets);
        }

        // KULLANICI: Yeni talep oluştursun
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SupportTicket ticket)
        {
            var user = await _userManager.GetUserAsync(User);
            ticket.UserId = user.Id;
            ticket.Status = "Beklemede";
            ticket.CreatedDate = DateTime.Now;

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // --- ADMIN İŞLEMLERİ ---

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminList()
        {
            // Tüm talepleri kullanıcı bilgisiyle getir
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return View(tickets);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Reply(int id, string adminResponse)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.AdminResponse = adminResponse;
                ticket.Status = "Cevaplandı"; // Durumu güncelle
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AdminList");
        }
    }
}