using Ecommerce_Mvc.Data;
using Ecommerce_Mvc.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog.Events;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

//Creates a new WebApplicationBuilder for configuring the web application.
var builder = WebApplication.CreateBuilder(args);

//Cache
builder.Services.AddMemoryCache();


//Configures Serilog for logging with specific settings:
//Writes log events to the console.
//Writes log events to a file (Imanelogg.txt) with daily log rolling.

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
   // .WriteTo.Console()
    .WriteTo.File("./Log/logfile.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

//Registers services for controllers and views.
builder.Services.AddControllersWithViews();

//Registers the ApplicationDbContext for Entity Framework Core with SQLite.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppContextDB") ?? string.Empty)
);


//Configures and adds session services with a 30-minute idle timeout and essential cookie settings.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
});


//Configures and adds default Identity services for user authentication, using the ApplicationUser class and the ApplicationDbContext for storage.
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();




var app = builder.Build();

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
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();

