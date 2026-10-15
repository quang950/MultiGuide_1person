using System.Reflection;
using MultiGuide.Domain.Entities;
using Xunit;

namespace MultiGuide.Architecture.Tests;

/// <summary>
/// Kiểm tra quy tắc bảo mật và quyền riêng tư (T-3.14):
/// Tuyệt đối không lưu toạ độ GPS của du khách và không lưu raw token trong CSDL.
/// </summary>
public class SecurityPrivacyArchitectureTests
{
    [Fact]
    public void AccessPass_ChiLuuTokenHash_TuyetDoiKhongCoPropertyRawToken()
    {
        var passType = typeof(AccessPass);
        var properties = passType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        // Bắt buộc phải có token_hash (SHA-256)
        Assert.Contains("tokenhash", properties);

        // Tuyệt đối không có thuộc tính chứa token thô
        var forbiddenTokenNames = new[] { "token", "rawtoken", "accesstoken", "plaintoken" };
        foreach (var forbidden in forbiddenTokenNames)
        {
            Assert.DoesNotContain(forbidden, properties);
        }
    }

    [Fact]
    public void PlayLog_TuyetDoiKhongChuaToaDoGPS()
    {
        var playLogType = typeof(PlayLog);
        var properties = playLogType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        // Tuyệt đối không được lưu toạ độ GPS của du khách trong bảng PlayLog
        var forbiddenCoordNames = new[] { "lat", "lng", "latitude", "longitude", "userlat", "userlng", "coordinates" };
        foreach (var forbidden in forbiddenCoordNames)
        {
            Assert.DoesNotContain(forbidden, properties);
        }
    }

    [Fact]
    public void DuLieuKhachHang_PaymentVaAccessPassVaPlayLog_KhongChuaToaDoGps()
    {
        var userGeneratedTypes = new[]
        {
            typeof(Payment),
            typeof(AccessPass),
            typeof(PlayLog)
        };

        var forbiddenCoordNames = new[] { "lat", "lng", "latitude", "longitude", "userlat", "userlng", "coordinates" };

        foreach (var type in userGeneratedTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name.ToLowerInvariant())
                .ToList();

            foreach (var forbidden in forbiddenCoordNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }
    }
}
