namespace CinemaWeb.Models
{
    public class AdminComboItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminComboRequestModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminCombosListViewModel
    {
        public List<AdminComboItemViewModel> Combos { get; set; } = new();
        public string? SearchKeyword { get; set; }
    }
}
