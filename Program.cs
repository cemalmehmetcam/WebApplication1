using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. SQL Server Baðlantýsý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity Servislerinin Eklenmesi (Cookie Ayarlarý yerine bu geliyor)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Þifre kurallarýný proje geliþtirme aþamasýnda kolaylýk olsun diye esnetiyoruz.
    // Ýstersen daha sonra burayý sýkýlaþtýrabilirsin.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Repository Dependency Injection Tanýmlarý
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Hata yönetimi ve HSTS (Production ortamý için standart kodlar)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Önce kimlik doðrulama, sonra yetkilendirme çalýþmalý
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<WebApplication1.Hubs.OrderHub>("/orderhub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await SeedData.Initialize(scope.ServiceProvider);
}

app.Run();