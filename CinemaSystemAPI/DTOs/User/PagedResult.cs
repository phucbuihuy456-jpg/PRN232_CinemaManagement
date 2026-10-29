namespace CinemaSystemAPI.DTOs.User
{
    public class PagedResult
    {
        public List<UserDTO> Items { get; set; } = new List<UserDTO>();
        public int TotalItems { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems/ PageSize);
    }
}
