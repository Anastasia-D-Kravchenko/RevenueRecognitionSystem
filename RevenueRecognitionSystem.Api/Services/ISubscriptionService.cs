using RevenueRecognitionSystem.Api.DTOs;

namespace RevenueRecognitionSystem.Api.Services;

public interface ISubscriptionService
{
    Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto);
    Task<SubscriptionDto> PayRenewalAsync(int subscriptionId, PaySubscriptionDto dto);
    Task<SubscriptionDto> GetAsync(int subscriptionId);
}
