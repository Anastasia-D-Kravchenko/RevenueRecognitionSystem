using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Exceptions;

namespace RevenueRecognitionSystem.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private const decimal LoyalCustomerDiscountPercent = 5m;

    private readonly AppDbContext _db;
    private readonly DiscountPolicy _discounts;

    public SubscriptionService(AppDbContext db, DiscountPolicy discounts)
    {
        _db = db;
        _discounts = discounts;
    }

    public async Task<SubscriptionDto> CreateAsync(CreateSubscriptionDto dto)
    {
        if (dto.RenewalPeriodMonths < 1 || dto.RenewalPeriodMonths > 24)
            throw new BadRequestException("Renewal period must be between 1 month and 24 months.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId);
        if (customer is null)
            throw new NotFoundException($"Customer with id={dto.CustomerId} was not found.");
        if (customer is IndividualCustomer { IsDeleted: true })
            throw new ConflictException("Customer has been deleted.");

        var software = await _db.Softwares.FirstOrDefaultAsync(s => s.SoftwareId == dto.SoftwareId);
        if (software is null)
            throw new NotFoundException($"Software with id={dto.SoftwareId} was not found.");

        var now = DateTime.UtcNow;

        var hasActive = await _db.Subscriptions.AnyAsync(s =>
            s.CustomerId == dto.CustomerId
            && s.SoftwareId == dto.SoftwareId
            && s.Status == SubscriptionStatus.Active);
        var hasActiveContract = await _db.Contracts.AnyAsync(c =>
            c.CustomerId == dto.CustomerId
            && c.SoftwareId == dto.SoftwareId
            && c.Status == ContractStatus.Pending
            && c.EndDate >= now);
        if (hasActive || hasActiveContract)
            throw new ConflictException(
                "Customer already has an active subscription or contract for this product.");

        var isReturning =
            await _db.Contracts.AnyAsync(c => c.CustomerId == dto.CustomerId && c.Status == ContractStatus.Signed)
            || await _db.Subscriptions.AnyAsync(s => s.CustomerId == dto.CustomerId);

        var discounts = await _db.Discounts.ToListAsync();
        var promo = _discounts.PickHighestActive(discounts, DiscountOffer.Subscription, now);
        var loyalty = isReturning ? LoyalCustomerDiscountPercent : 0m;
        var totalDiscountPercent = Math.Min(promo + loyalty, 100m);
        var firstPayment = Math.Round(dto.RenewalPrice * (1m - totalDiscountPercent / 100m), 2);

        var sub = new Subscription
        {
            Name = dto.Name,
            CustomerId = dto.CustomerId,
            SoftwareId = dto.SoftwareId,
            RenewalPeriodMonths = dto.RenewalPeriodMonths,
            RenewalPrice = dto.RenewalPrice,
            StartDate = now,
            CurrentPeriodEnd = now.AddMonths(dto.RenewalPeriodMonths),
            Status = SubscriptionStatus.Active
        };
        sub.Payments.Add(new SubscriptionPayment
        {
            Amount = firstPayment,
            PaidAt = now,
            PeriodStart = sub.StartDate,
            PeriodEnd = sub.CurrentPeriodEnd
        });
        _db.Subscriptions.Add(sub);
        await _db.SaveChangesAsync();

        return ToDto(sub);
    }

    public async Task<SubscriptionDto> PayRenewalAsync(int subscriptionId, PaySubscriptionDto dto)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        if (sub is null)
            throw new NotFoundException($"Subscription with id={subscriptionId} was not found.");

        var now = DateTime.UtcNow;

        if (sub.Status == SubscriptionStatus.Cancelled)
            throw new ConflictException("Subscription is cancelled.");

        var periodStart = sub.StartDate;
        var n = 1;
        while (periodStart.AddMonths(sub.RenewalPeriodMonths) <= now)
        {
            periodStart = periodStart.AddMonths(sub.RenewalPeriodMonths);
            n++;
        }
        var periodEnd = periodStart.AddMonths(sub.RenewalPeriodMonths);

        var paidCount = sub.Payments.Count;

        if (paidCount < n - 1)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            await _db.SaveChangesAsync();
            throw new ConflictException(
                "Subscription is overdue (a previous renewal period was not paid). It has been cancelled.");
        }

        if (paidCount >= n)
            throw new ConflictException("Current renewal period has already been paid.");

        var expected = Math.Round(sub.RenewalPrice * (1m - LoyalCustomerDiscountPercent / 100m), 2);
        if (dto.Amount != expected)
            throw new BadRequestException(
                $"Renewal payment must equal {expected} (loyal customer discount applied).");

        _db.SubscriptionPayments.Add(new SubscriptionPayment
        {
            SubscriptionId = sub.SubscriptionId,
            Amount = dto.Amount,
            PaidAt = now,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });
        sub.CurrentPeriodEnd = periodEnd;
        await _db.SaveChangesAsync();

        return ToDto(sub);
    }

    public async Task<SubscriptionDto> GetAsync(int subscriptionId)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        if (sub is null)
            throw new NotFoundException($"Subscription with id={subscriptionId} was not found.");
        return ToDto(sub);
    }

    private static SubscriptionDto ToDto(Subscription s) => new(
        s.SubscriptionId, s.Name, s.CustomerId, s.SoftwareId,
        s.RenewalPeriodMonths, s.RenewalPrice, s.CurrentPeriodEnd, s.Status.ToString());
}
