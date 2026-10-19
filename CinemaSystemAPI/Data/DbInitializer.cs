using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Data
{
    public static class DbInitializer
    {
        public static void SeedData(CinemaDbContext context)
        {
            if (!context.Combos.Any())
            {
                var sampleCombos = new List<Combo>
                {
                    new Combo
                    {
                        Name = "Combo Solo",
                        Description = "1 Bắp ngọt vừa + 1 Nước ngọt có ga vừa",
                        Price = 85000,
                        ImageUrl = "https://images.unsplash.com/photo-1585647347384-2593bc35786b?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo Couple",
                        Description = "1 Bắp ngọt lớn + 2 Nước ngọt có ga lớn",
                        Price = 119000,
                        ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo Party Family",
                        Description = "2 Bắp ngọt lớn + 4 Nước ngọt có ga lớn + 1 Khoai tây lắc phô mai",
                        Price = 209000,
                        ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo VIP Caramel Cheese",
                        Description = "1 Bắp phô mai/caramel lớn + 2 Nước ép hoa quả",
                        Price = 139000,
                        ImageUrl = "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    }
                };

                context.Combos.AddRange(sampleCombos);
                context.SaveChanges();
            }
        }
    }
}
