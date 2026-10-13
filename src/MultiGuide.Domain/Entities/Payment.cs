using MultiGuide.Domain.Enums;

namespace MultiGuide.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AreaId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Provider { get; set; } = "mock";
    public string? ProviderRef { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TourArea? Area { get; set; }
    public AccessPass? AccessPass { get; set; }
}
