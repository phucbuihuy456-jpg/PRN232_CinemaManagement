using QRCoder;

namespace CinemaSystemAPI.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        public byte[] GenerateQrCodePng(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<byte>();

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }
}

