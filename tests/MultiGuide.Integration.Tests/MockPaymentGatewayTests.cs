using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiGuide.Business.Payments;
using MultiGuide.Data.Payments;

namespace MultiGuide.Integration.Tests;

public class MockPaymentGatewayTests
{
    private static MockPaymentGateway CreateGateway(string? mockResultConfig = null)
    {
        var inMemorySettings = new Dictionary<string, string?>();
        if (mockResultConfig != null)
        {
            inMemorySettings["Payment:MockResult"] = mockResultConfig;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new MockPaymentGateway(configuration, NullLogger<MockPaymentGateway>.Instance);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NullRequest_NemArgumentNullException()
    {
        // Arrange
        var gateway = CreateGateway();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => gateway.ProcessPaymentAsync(null!));
    }

    [Fact]
    public async Task ProcessPaymentAsync_MacDinhKhongCauHinh_TraVeThanhCong()
    {
        // Arrange
        var gateway = CreateGateway();
        var request = new PaymentRequest
        {
            Amount = 10.0m,
            Currency = "USD",
            IdempotencyKey = "key-default-success",
            AreaId = Guid.NewGuid()
        };

        // Act
        var result = await gateway.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("mock", result.Provider);
        Assert.False(string.IsNullOrWhiteSpace(result.ProviderRef));
        Assert.StartsWith("mock_tx_", result.ProviderRef);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("succeeded")]
    [InlineData("SUCCESS")]
    [InlineData("SUCCEEDED")]
    public async Task ProcessPaymentAsync_CauHinhSuccess_TraVeThanhCong(string configValue)
    {
        // Arrange
        var gateway = CreateGateway(configValue);
        var request = new PaymentRequest
        {
            Amount = 5.0m,
            Currency = "USD",
            IdempotencyKey = "key-success-test",
            AreaId = Guid.NewGuid()
        };

        // Act
        var result = await gateway.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("mock", result.Provider);
        Assert.StartsWith("mock_tx_", result.ProviderRef!);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("failure")]
    [InlineData("FAILED")]
    [InlineData("Failure")]
    public async Task ProcessPaymentAsync_CauHinhFailed_TraVeThatBai(string configValue)
    {
        // Arrange
        var gateway = CreateGateway(configValue);
        var request = new PaymentRequest
        {
            Amount = 5.0m,
            Currency = "USD",
            IdempotencyKey = "key-failed-test",
            AreaId = Guid.NewGuid()
        };

        // Act
        var result = await gateway.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("mock", result.Provider);
        Assert.Null(result.ProviderRef);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task ProcessPaymentAsync_MoiGiaoDichThanhCong_CoProviderRefDuyNhat()
    {
        // Arrange
        var gateway = CreateGateway("success");
        var request = new PaymentRequest
        {
            Amount = 15.0m,
            Currency = "USD",
            IdempotencyKey = "key-unique",
            AreaId = Guid.NewGuid()
        };

        // Act
        var result1 = await gateway.ProcessPaymentAsync(request);
        var result2 = await gateway.ProcessPaymentAsync(request);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.ProviderRef, result2.ProviderRef);
    }
}
