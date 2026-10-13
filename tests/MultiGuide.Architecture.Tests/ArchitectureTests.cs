using NetArchTest.Rules;
using Xunit;

namespace MultiGuide.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly string DomainNamespace = typeof(Domain.AssemblyMarker).Namespace!;
    private static readonly string BusinessNamespace = typeof(Business.AssemblyMarker).Namespace!;
    private static readonly string DataNamespace = typeof(Data.AssemblyMarker).Namespace!;
    private static readonly string ApiNamespace = typeof(Api.AssemblyMarker).Namespace!;

    [Fact]
    public void Domain_Khong_Phu_Thuoc_Bat_Ky_Project_Nao()
    {
        var result = Types.InAssembly(typeof(Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(BusinessNamespace, DataNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain không được phụ thuộc vào Business, Data hoặc Api.");
    }

    [Fact]
    public void Business_Khong_Phu_Thuoc_Data_Va_Api()
    {
        var result = Types.InAssembly(typeof(Business.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(DataNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Business không được phụ thuộc vào Data hoặc Api.");
    }

    [Fact]
    public void Business_khong_biet_Data_Api_EFCore_Npgsql_Http()
    {
        var prohibitedDependencies = new[]
        {
            DataNamespace,
            ApiNamespace,
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "System.Net.Http"
        };

        var result = Types.InAssembly(typeof(Business.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(prohibitedDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, "Business không được phụ thuộc vào Data, Api, EF Core, Npgsql hoặc System.Net.Http.");
    }

    [Fact]
    public void Data_Khong_Phu_Thuoc_Api()
    {
        var result = Types.InAssembly(typeof(Data.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Data không được phụ thuộc vào Api.");
    }

    [Fact]
    public void Api_Phu_Thuoc_Dung_Kien_Truc()
    {
        // Các thành phần trong Api (Controllers, Middlewares...) không được phụ thuộc Data implementation (ngoại trừ Composition Root Program cấu hình DI)
        var result = Types.InAssembly(typeof(Api.AssemblyMarker).Assembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(DataNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, "Api (ngoại trừ Composition Root Program) không được phụ thuộc Data.");
    }

    [Fact]
    public void Api_Controllers_Khong_Phu_Thuoc_Data_Va_DbContext()
    {
        var result = Types.InAssembly(typeof(Api.AssemblyMarker).Assembly)
            .That()
            .ResideInNamespace("MultiGuide.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(DataNamespace, "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Controllers không được phụ thuộc trực tiếp Data hoặc DbContext.");
    }
}
