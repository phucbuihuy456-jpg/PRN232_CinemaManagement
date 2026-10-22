using CinemaSystemAPI.Data;
using CinemaSystemAPI.Models;
using CinemaSystemAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CinemaSystemAPI", Version = "v1" });
});

// Configure DbContext
builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// Register Background Services (Task 8: ExpiredSeatHoldCleanupService chạy mỗi 30s)
builder.Services.AddHostedService<ExpiredSeatHoldCleanupService>();

var app = builder.Build();

// Seed initial master data (Combos, SeatTypes, Rooms, Seats, Movies, Showtimes)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    DbInitializer.SeedData(context);
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
