using Drought.Application.DTOs;

namespace Drought.Application.Interfaces;

public interface ITrainingService
{
    /// <summary>Exporta as observações rotuladas, treina o(s) modelo(s), guarda métricas
    /// e activa o melhor modelo (por MacroAccuracy) — secção "5.3 Qualidade da IA".</summary>
    Task<IReadOnlyList<ModelMetricsDto>> TrainAndCompareAsync(string triggeredByUserId);
    Task<ModelMetricsDto?> GetActiveModelMetricsAsync();
}
