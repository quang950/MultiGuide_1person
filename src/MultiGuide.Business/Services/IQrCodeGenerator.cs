namespace MultiGuide.Business.Services;

/// <summary>
/// Dịch vụ sinh hình ảnh mã QR (US-14).
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    /// Sinh chuỗi byte ảnh PNG của mã QR từ nội dung văn bản hoặc URL.
    /// </summary>
    byte[] GeneratePng(string content, int pixelsPerModule = 20);
}
