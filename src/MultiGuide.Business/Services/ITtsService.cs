namespace MultiGuide.Business.Services;

/// <summary>
/// Cung cấp dịch vụ chuyển văn bản thành giọng đọc (Text-to-Speech) chuẩn Neural.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Chuyển đổi văn bản sang định dạng âm thanh MP3.
    /// </summary>
    /// <param name="text">Nội dung văn bản cần đọc</param>
    /// <param name="languageCode">Mã ngôn ngữ (vi, en, ja, zh...)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dữ liệu nhị phân của file MP3</returns>
    Task<byte[]> SynthesizeSpeechAsync(string text, string languageCode, CancellationToken cancellationToken = default);
}
