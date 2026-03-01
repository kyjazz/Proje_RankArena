using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Services;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Identity Razor Pages
builder.Services.AddRazorPages();

// EF Core - SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // ✅ Email onayı zorunlu
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;

        // Şifre kuralları (güçlü şifre)
        options.Password.RequireNonAlphanumeric = true; // özel karakter
        options.Password.RequireUppercase = true;       // büyük harf
        options.Password.RequireLowercase = true;       // küçük harf
        options.Password.RequireDigit = true;           // rakam
        options.Password.RequiredLength = 6;

        // ✅ Brute-force koruması (Lockout)
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Kullanıcı ayarları
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ✅ SMTP ayarlarını oku
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// ✅ Gerçek email gönderici
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ✅ Cookie / Remember Me güvenliği
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";

    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "RankArena.Auth";
});

// ✅ Token ömrü (şifre sıfırlama, email onay tokenları)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(3);
});

var app = builder.Build();

// Admin + User rol seed
await SeedIdentityAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Identity Razor Pages map
app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ----------------------------------------------------
// Admin + User Seed (rol + kullanıcı)
// ----------------------------------------------------
static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    const string adminEmail = "";
    const string adminPassword = "";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
            throw new Exception("Admin user oluşturulamadı: " + errors);
        }

        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}