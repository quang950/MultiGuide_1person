using System.Text.RegularExpressions;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class AdminContentService : IAdminContentService
{
    private static readonly Regex CodeRegex = new(@"^[a-z0-9-]+$", RegexOptions.Compiled);
    private readonly IAreaRepository _areaRepository;

    public AdminContentService(IAreaRepository areaRepository)
    {
        _areaRepository = areaRepository;
    }

    public async Task<IReadOnlyList<AreaResponse>> GetAreasAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var areas = await _areaRepository.GetAllAsync(isActive, cancellationToken);
        return areas.Select(MapToResponse).ToList();
    }

    public async Task<AreaResponse> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        return MapToResponse(area);
    }

    public async Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var codeNormalized = request.Code.Trim();
        var isDuplicate = await _areaRepository.ExistsByCodeAsync(codeNormalized, null, cancellationToken);
        if (isDuplicate)
        {
            throw new ConflictException(ErrorCodes.DuplicateRequest, $"Khu với mã '{codeNormalized}' đã tồn tại.");
        }

        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = codeNormalized,
            Name = request.Name.Trim(),
            CenterLat = request.CenterLat,
            CenterLng = request.CenterLng,
            RadiusM = request.RadiusM,
            PriceUsd = request.PriceUsd,
            PassTtlHours = request.PassTtlHours,
            IsActive = true
        };

        var created = await _areaRepository.AddAsync(area, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<AreaResponse> UpdateAreaAsync(Guid id, UpdateAreaRequest request, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        ValidateUpdateRequest(request);

        area.Name = request.Name.Trim();
        area.CenterLat = request.CenterLat;
        area.CenterLng = request.CenterLng;
        area.RadiusM = request.RadiusM;
        area.PriceUsd = request.PriceUsd;
        area.PassTtlHours = request.PassTtlHours;
        area.IsActive = request.IsActive;

        await _areaRepository.UpdateAsync(area, cancellationToken);
        return MapToResponse(area);
    }

    public async Task DeleteAreaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        area.IsActive = false;
        await _areaRepository.UpdateAsync(area, cancellationToken);
    }

    private static void ValidateCreateRequest(CreateAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) là bắt buộc.");
        }

        var trimmedCode = request.Code.Trim();
        if (trimmedCode.Length > 50)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) không được vượt quá 50 ký tự.");
        }

        if (!CodeRegex.IsMatch(trimmedCode))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) chỉ được chứa chữ cái thường, số và dấu gạch ngang (-).");
        }

        ValidateCommonFields(request.Name, request.PriceUsd, request.CenterLat, request.CenterLng, request.RadiusM, request.PassTtlHours);
    }

    private static void ValidateUpdateRequest(UpdateAreaRequest request)
    {
        ValidateCommonFields(request.Name, request.PriceUsd, request.CenterLat, request.CenterLng, request.RadiusM, request.PassTtlHours);
    }

    private static void ValidateCommonFields(string name, decimal priceUsd, double centerLat, double centerLng, double radiusM, int passTtlHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Tên khu (name) là bắt buộc.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Tên khu (name) không được vượt quá 200 ký tự.");
        }

        if (priceUsd <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Giá vé (price_usd) phải lớn hơn 0.");
        }

        if (centerLat is < -90 or > 90)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Vĩ độ trung tâm (center_lat) phải nằm trong khoảng [-90, 90].");
        }

        if (centerLng is < -180 or > 180)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Kinh độ trung tâm (center_lng) phải nằm trong khoảng [-180, 180].");
        }

        if (radiusM <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Bán kính hoạt động (radius_m) phải lớn hơn 0.");
        }

        if (passTtlHours <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Thời hạn hiệu lực vé (pass_ttl_hours) phải lớn hơn 0.");
        }
    }

    private static AreaResponse MapToResponse(TourArea area) => new()
    {
        Id = area.Id,
        Code = area.Code,
        Name = area.Name,
        CenterLat = area.CenterLat,
        CenterLng = area.CenterLng,
        RadiusM = area.RadiusM,
        PriceUsd = area.PriceUsd,
        PassTtlHours = area.PassTtlHours,
        IsActive = area.IsActive
    };
}
