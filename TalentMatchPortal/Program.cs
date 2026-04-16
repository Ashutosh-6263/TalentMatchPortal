using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TalentMatchPortal.Data;

var builder = WebApplication.CreateBuilder(args);



// --- ADD THIS SECTION ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Where to go if not logged in
        options.AccessDeniedPath = "/Account/AccessDenied"; // Where to go if role doesn't match
        options.LogoutPath = "/Account/Logout";
    });
// ------------------------

builder.Services.AddControllersWithViews();
// ... other services like DbContext and Session ...


// --- 1. Register Services HERE (Before Build) ---
builder.Services.AddControllersWithViews();

// Move these two blocks up here:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();
// ------------------------------------------------

var app = builder.Build();

// --- 2. Configure Middleware (After Build) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// UseSession must come after UseRouting but before MapControllerRoute
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();