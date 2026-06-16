namespace RevenueRecognitionSystem.Api.DTOs;

public record TokenDto(string AccessToken, DateTime ExpiresAt, string Role);

public record CustomerListItemDto(
    int CustomerId,
    string Type,
    string Display,
    string Email,
    string PhoneNumber,
    bool IsDeleted);

public record ContractDto(
    int ContractId,
    int CustomerId,
    int SoftwareId,
    string SoftwareVersion,
    DateTime StartDate,
    DateTime EndDate,
    int SupportYears,
    decimal TotalPrice,
    decimal AmountPaid,
    string Status,
    DateTime? SignedAt);

public record SubscriptionDto(
    int SubscriptionId,
    string Name,
    int CustomerId,
    int SoftwareId,
    int RenewalPeriodMonths,
    decimal RenewalPrice,
    DateTime CurrentPeriodEnd,
    string Status);

public record RevenueDto(
    decimal Amount,
    string Currency,
    string Scope,
    int? SoftwareId);
