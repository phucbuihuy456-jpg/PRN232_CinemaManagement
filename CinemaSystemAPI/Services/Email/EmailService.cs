using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CinemaSystemAPI.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    EnableSsl = _smtpSettings.EnableSsl,
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.Username, _smtpSettings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Đã gửi email thành công tới {Email} với chủ đề: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email qua SMTP tới {Email}", toEmail);
                throw new InvalidOperationException($"Không thể gửi email tới {toEmail}: {ex.Message}", ex);
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            string subject = "CINEMA - Mã OTP Đặt Lại Mật Khẩu Của Bạn";

            string htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #141414;
            color: #ffffff;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 520px;
            margin: 0 auto;
            background-color: #1f1f1f;
            border-radius: 12px;
            overflow: hidden;
            border: 1px solid #333333;
        }}
        .header {{
            background: linear-gradient(135deg, #e50914 0%, #b81d24 100%);
            padding: 25px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            color: #ffffff;
            font-size: 26px;
            letter-spacing: 2px;
            text-transform: uppercase;
        }}
        .content {{
            padding: 30px;
            color: #e5e5e5;
            line-height: 1.6;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 15px;
        }}
        .otp-box {{
            background-color: #2b2b2b;
            border: 2px dashed #e50914;
            border-radius: 8px;
            padding: 18px;
            text-align: center;
            margin: 25px 0;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 800;
            letter-spacing: 8px;
            color: #e50914;
            margin: 0;
            font-family: monospace;
        }}
        .expire-text {{
            font-size: 13px;
            color: #ffc107;
            margin-top: 8px;
        }}
        .footer {{
            background-color: #181818;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #888888;
            border-top: 1px solid #2a2a2a;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎬 CINEMA SYSTEM</h1>
        </div>
        <div class=""content"">
            <p class=""greeting"">Xin chào <strong>{fullName}</strong>,</p>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email này. Vui lòng sử dụng mã OTP dưới đây để hoàn tất quá trình đổi mật khẩu:</p>
            
            <div class=""otp-box"">
                <div class=""otp-code"">{otpCode}</div>
                <div class=""expire-text"">⏱️ Mã có hiệu lực trong vòng <strong>5 phút</strong></div>
            </div>

            <p style=""font-size: 13px; color: #aaaaaa;"">
                ⚠️ <strong>Lưu ý bảo mật:</strong> Tuyệt đối không cung cấp mã OTP này cho bất kỳ ai, kể cả nhân viên rạp chiếu phim. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email và bảo mật lại tài khoản của mình.
            </p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.Now.Year} Cinema System - Hệ thống đặt vé xem phim trực tuyến.</p>
            <p>Email này được gửi tự động, vui lòng không trả lời thư này.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}
