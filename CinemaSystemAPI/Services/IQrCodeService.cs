namespace CinemaSystemAPI.Services
{
    public interface IQrCodeService
    {
        string GenerateQrCodeBase64(string text);
        byte[] GenerateQrCodePng(string text);
    }
}

