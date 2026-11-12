namespace CinemaWeb.Models
{
    public class AdminUserItemViewModel
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUsersListViewModel
    {
        public List<AdminUserItemViewModel> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));
        public string? SearchKeyword { get; set; }
        public string? SelectedRole { get; set; }
        public bool? SelectedIsActive { get; set; }
        public int CurrentUserId { get; set; }
    }

    public class UpdateUserRoleRequestModel
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserStatusRequestModel
    {
        public bool IsActive { get; set; }
    }
}
