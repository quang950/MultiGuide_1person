namespace MultiGuide.Business.Storage;

/// <summary>
/// Cung cấp các thao tác lưu trữ file trong hệ thống (Supabase Storage bucket private).
/// Tuân thủ nguyên lý Dependency Inversion: Business định nghĩa interface, Data hiện thực.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Tải stream nội dung lên Storage theo đường dẫn tương đối trong bucket.
    /// Trả về đường dẫn tương đối của file đã lưu (ví dụ: "pois/{poiId}/{lang}.mp3").
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string path,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file khỏi Storage theo đường dẫn tương đối. Thao tác có tính idempotent (không ném lỗi nếu file không tồn tại).
    /// </summary>
    Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo Signed URL cho phép client đọc/nghe file trong private bucket trong khoảng thời gian xác định.
    /// </summary>
    Task<string> CreateSignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
