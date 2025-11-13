using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var textEntries = _context.TextEntries.ToList();
        return View(textEntries);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult GurkanFikretGunak()
    {
        ViewData["Message"] = "Gürkan Fikret Günak";
        return View();
    }

    [HttpPost]
    public IActionResult SaveText(string content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            var textEntry = new TextEntry { Content = content };
            _context.TextEntries.Add(textEntry);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteText(int id)
    {
        var textEntry = _context.TextEntries.FirstOrDefault(e => e.Id == id);
        if (textEntry != null)
        {
            _context.TextEntries.Remove(textEntry);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
