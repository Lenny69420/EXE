using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuatBook.Helpers;
using QuatBook.Hubs;
using QuatBook.Models;
using QuatBook.Service;
using System;


internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();


        builder.Services.AddDbContext<QuatBookContext>(option =>
            option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Thêm Identity
        //builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        //    .AddEntityFrameworkStores<QuatBookContext>()
        //    .AddDefaultTokenProviders();

        //signalR
        builder.Services.AddSignalR();
        //cookie
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";// đã đăng nhập nhưng không có quyền truy cập
            });
        //config session
        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(10);//session hết hạn sau 10 giây nếu không có hoạt động nào từ người dùng.
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddSingleton<IVnPayService, VnPayService>();
        builder.Services.AddHttpClient<IVietQRService, VietQRService>();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        //session
        app.UseSession();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Product}/{action=Index}/{id?}");
        // Thêm endpoint cho SignalR Hub
        app.MapHub<ProductHub>("/productHub");
        app.Run();
    }
}