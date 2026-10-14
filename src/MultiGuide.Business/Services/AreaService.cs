using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class AreaService : IAreaService
{
    private readonly IAreaRepository _areaRepository;

    public AreaService(IAreaRepository areaRepository)
    {
        _areaRepository = areaRepository;
    }

    public async Task<AreaDetailResponse> GetAreaByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, "Mã khu tham quan không được để trống.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        var area = await _areaRepository.GetByCodeAsync(normalizedCode, cancellationToken);

        if (area == null || !area.IsActive)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Khu tham quan với mã '{code}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        var languages = await _areaRepository.GetSupportedLanguagesAsync(area.Id, cancellationToken);

        return new AreaDetailResponse
        {
            Id = area.Id,
            Code = area.Code,
            Name = area.Name,
            CenterLat = area.CenterLat,
            CenterLng = area.CenterLng,
            RadiusM = area.RadiusM,
            PriceUsd = area.PriceUsd,
            PassTtlHours = area.PassTtlHours,
            SupportedLanguages = languages.Select(l => new SupportedLanguageDto
            {
                Code = l.Code,
                Name = l.Name
            }).ToList()
        };
    }
}
