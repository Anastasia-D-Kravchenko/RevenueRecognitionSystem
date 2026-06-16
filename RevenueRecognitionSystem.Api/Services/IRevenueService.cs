using RevenueRecognitionSystem.Api.DTOs;

namespace RevenueRecognitionSystem.Api.Services;

public interface IRevenueService
{
    Task<RevenueDto> GetCurrentAsync(int? softwareId, string currency, CancellationToken ct = default);
    Task<RevenueDto> GetPredictedAsync(int? softwareId, string currency, CancellationToken ct = default);
}
