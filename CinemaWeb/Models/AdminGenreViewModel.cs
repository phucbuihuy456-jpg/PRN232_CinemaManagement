namespace CinemaWeb.Models
{
    public class AdminGenreItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MovieCount { get; set; }
    }

    public class AdminGenreRequestModel
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AdminGenresListViewModel
    {
        public List<AdminGenreItemViewModel> Genres { get; set; } = new();
        public string? SearchKeyword { get; set; }
    }
}
