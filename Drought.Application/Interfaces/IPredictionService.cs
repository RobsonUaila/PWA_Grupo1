using Drought.Application.DTOs;

namespace Drought.Application.Interfaces;

public interface IPredictionService
{
    Task<PredictionResultDto> PredictAsync(PredictionRequestDto request, string userId);
    Task<IReadOnlyList<PredictionResultDto>> GetHistoryAsync(int? regionId, DateOnly? from, DateOnly? to);
}
