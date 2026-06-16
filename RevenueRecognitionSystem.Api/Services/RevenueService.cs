using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Services;

public class RevenueService : IRevenueService
{
    private readonly AppDbContext _db;
    private readonly NbpExchangeRateProvider _exchange;

    public RevenueService(AppDbContext db, NbpExchangeRateProvider exchange)
    {
        _db = db;
        _exchange = exchange;
    }

    public async Task<RevenueDto> GetCurrentAsync(int? softwareId, string currency, CancellationToken ct = default)
    {
        var signedContractsQuery = _db.Contracts.Where(c => c.Status == ContractStatus.Signed);
        if (softwareId.HasValue)
            signedContractsQuery = signedContractsQuery.Where(c => c.SoftwareId == softwareId.Value);
        var contractRevenue = await signedContractsQuery.SumAsync(c => (decimal?)c.TotalPrice, ct) ?? 0m;

        var subPaymentsQuery = _db.SubscriptionPayments.AsQueryable();
        if (softwareId.HasValue)
            subPaymentsQuery = subPaymentsQuery.Where(p => p.Subscription.SoftwareId == softwareId.Value);
        var subscriptionRevenue = await subPaymentsQuery.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var totalPln = contractRevenue + subscriptionRevenue;
        var (amount, code) = await ConvertAsync(totalPln, currency, ct);

        return new RevenueDto(amount, code, "Current", softwareId);
    }

    public async Task<RevenueDto> GetPredictedAsync(int? softwareId, string currency, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var contractsQuery = _db.Contracts.Where(c =>
            c.Status == ContractStatus.Signed
            || (c.Status == ContractStatus.Pending && c.EndDate >= now));
        if (softwareId.HasValue)
            contractsQuery = contractsQuery.Where(c => c.SoftwareId == softwareId.Value);
        var contractTotal = await contractsQuery.SumAsync(c => (decimal?)c.TotalPrice, ct) ?? 0m;

        var activeSubsQuery = _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd >= now);
        if (softwareId.HasValue)
            activeSubsQuery = activeSubsQuery.Where(s => s.SoftwareId == softwareId.Value);

        var activeSubs = await activeSubsQuery
            .Select(s => new { s.RenewalPrice, s.RenewalPeriodMonths })
            .ToListAsync(ct);

        var subForecast = activeSubs.Sum(s => s.RenewalPrice * (12 / s.RenewalPeriodMonths));

        var totalPln = contractTotal + subForecast;
        var (amount, code) = await ConvertAsync(totalPln, currency, ct);
        return new RevenueDto(amount, code, "Predicted", softwareId);
    }

    private async Task<(decimal amount, string code)> ConvertAsync(decimal pln, string currency, CancellationToken ct)
    {
        var code = (currency ?? "PLN").ToUpperInvariant();
        if (code == "PLN")
            return (Math.Round(pln, 2), code);
        var rate = await _exchange.GetRateAsync(code, ct);
        return (Math.Round(pln * rate, 2), code);
    }
}
