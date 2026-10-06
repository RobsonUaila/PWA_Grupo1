namespace Drought.Application.DTOs;

public record PredictionRequestDto(int RegionId, DateOnly Date, double S1_SoilMoisture,
    double? AscatSoilMoisture, double? Clay, double? Sand, double? OrganicCarbon, double? BulkDensity);

public record PredictionResultDto(
    string Region,
    DateOnly Date,
    string PredictedClass,
    double Confidence,
    Dictionary<string, float> Probabilities,
    string ModelAlgorithm,
    DateTime ModelTrainedAtUtc);

public record ModelMetricsDto(
    int ModelVersionId,
    string Algorithm,
    DateTime TrainedAtUtc,
    int TrainingRows,
    int TestRows,
    double MacroAccuracy,
    double MicroAccuracy,
    double LogLoss,
    bool IsActive);

public record RegionDto(int Id, string Name, string Province);

public record ObservationDto(
    DateOnly Date, string Region, double S1_SoilMoisture, double? AscatSoilMoisture,
    string? DroughtClass);
