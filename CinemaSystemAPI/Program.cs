using CinemaSystemAPI.Data;
using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

var app = builder.Build();

// Tự động seed dữ liệu mẫu khi khởi động ứng dụng
DbInitializer.Initialize(app.Services);

app.MapGet("/", () => "Hello World!");

app.Run();
