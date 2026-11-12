namespace CinemaSystemAPI.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
    }
}
