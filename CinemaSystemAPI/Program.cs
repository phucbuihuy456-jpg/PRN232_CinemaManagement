using CinemaSystemAPI.Data;
using CinemaSystemAPI.Models;
using CinemaSystemAPI.Services.Account;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
var app = builder.Build();

// Tự động seed dữ liệu mẫu khi khởi động ứng dụng
DbInitializer.Initialize(app.Services);

app.MapGet("/", () => "Hello World!");
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
