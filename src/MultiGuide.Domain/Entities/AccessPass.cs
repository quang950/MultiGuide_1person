using MultiGuide.Domain.Enums;

namespace MultiGuide.Domain.Entities;

public class AccessPass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AreaId { get; set; }
    public Guid PaymentId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public AccessPassStatus Status { get; set; } = AccessPassStatus.Active;

    // Navigation properties
    public TourArea? Area { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<PlayLog> PlayLogs { get; set; } = new List<PlayLog>();
}
