namespace CinemaSystemAPI.Services.Email
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderName { get; set; } = "Cinema System";
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
