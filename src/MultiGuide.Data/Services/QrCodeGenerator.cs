using MultiGuide.Business.Services;
using QRCoder;

namespace MultiGuide.Data.Services;

public class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 20)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }
}
