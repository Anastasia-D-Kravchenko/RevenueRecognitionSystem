using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Exceptions;
using RevenueRecognitionSystem.Api.Services;
using Xunit;

namespace RevenueRecognitionSystem.Tests;

public class SubscriptionServiceTests
{
    private static SubscriptionService NewService(out AppDbContext db)
    {
        db = TestHelpers.NewDb();
        return new SubscriptionService(db, new DiscountPolicy());
    }

    [Fact]
    public async Task Create_NewCustomer_FirstPaymentNoLoyalty()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        await service.CreateAsync(new CreateSubscriptionDto(1, 1, "Monthly", 1, 200m));
        var payment = Assert.Single(db.SubscriptionPayments);
        Assert.Equal(200m, payment.Amount);
    }

    [Fact]
    public async Task Create_ReturningCustomer_FirstPaymentWithLoyalty()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow.AddYears(-1).AddDays(10),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Signed
        });
        await db.SaveChangesAsync();
        await service.CreateAsync(new CreateSubscriptionDto(1, 1, "Monthly", 1, 200m));
        var payment = Assert.Single(db.SubscriptionPayments);
        Assert.Equal(190m, payment.Amount);
    }

    [Fact]
    public async Task Create_PeriodBelowMin_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateSubscriptionDto(1, 1, "Bad", 0, 100m);
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_PeriodAboveMax_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateSubscriptionDto(1, 1, "Bad", 25, 100m);
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_CustomerNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateSubscriptionDto(999, 1, "S", 1, 100m);
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_SoftwareNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateSubscriptionDto(1, 999, "S", 1, 100m);
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_DeletedCustomer_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.IndividualCustomers.Single().IsDeleted = true;
        await db.SaveChangesAsync();
        var dto = new CreateSubscriptionDto(1, 1, "S", 1, 100m);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ActiveSubscriptionExists_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Subscriptions.Add(new Subscription
        {
            Name = "Existing", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 100m,
            StartDate = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        });
        await db.SaveChangesAsync();
        var dto = new CreateSubscriptionDto(1, 1, "New", 1, 200m);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ActivePendingContractExists_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Pending
        });
        await db.SaveChangesAsync();
        var dto = new CreateSubscriptionDto(1, 1, "S", 1, 100m);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task PayRenewal_SamePeriodAsCreate_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var created = await service.CreateAsync(new CreateSubscriptionDto(1, 1, "S", 1, 200m));
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.PayRenewalAsync(created.SubscriptionId, new PaySubscriptionDto(190m)));
    }

    [Fact]
    public async Task PayRenewal_NextPeriodCorrectAmount_Accepts()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var start = DateTime.UtcNow.AddMonths(-1).AddDays(-1);
        var sub = new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 200m,
            StartDate = start, CurrentPeriodEnd = start.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        sub.Payments.Add(new SubscriptionPayment
        {
            Amount = 200m, PaidAt = start,
            PeriodStart = start, PeriodEnd = start.AddMonths(1)
        });
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        var result = await service.PayRenewalAsync(sub.SubscriptionId, new PaySubscriptionDto(190m));
        Assert.Equal("Active", result.Status);
        Assert.Equal(2, db.SubscriptionPayments.Count());
    }

    [Fact]
    public async Task PayRenewal_WrongAmount_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var start = DateTime.UtcNow.AddMonths(-1).AddDays(-1);
        var sub = new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 200m,
            StartDate = start, CurrentPeriodEnd = start.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        sub.Payments.Add(new SubscriptionPayment
        {
            Amount = 200m, PaidAt = start,
            PeriodStart = start, PeriodEnd = start.AddMonths(1)
        });
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.PayRenewalAsync(sub.SubscriptionId, new PaySubscriptionDto(200m)));
    }

    [Fact]
    public async Task PayRenewal_MissedPreviousPeriod_CancelsSubscription()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var start = DateTime.UtcNow.AddMonths(-6);
        var sub = new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 200m,
            StartDate = start, CurrentPeriodEnd = start.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        sub.Payments.Add(new SubscriptionPayment
        {
            Amount = 200m, PaidAt = start,
            PeriodStart = start, PeriodEnd = start.AddMonths(1)
        });
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.PayRenewalAsync(sub.SubscriptionId, new PaySubscriptionDto(190m)));
        Assert.Equal(SubscriptionStatus.Cancelled, db.Subscriptions.Single().Status);
    }

    [Fact]
    public async Task PayRenewal_CancelledSubscription_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var sub = new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 200m,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow,
            Status = SubscriptionStatus.Cancelled
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.PayRenewalAsync(sub.SubscriptionId, new PaySubscriptionDto(190m)));
    }

    [Fact]
    public async Task PayRenewal_SubscriptionNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.PayRenewalAsync(999, new PaySubscriptionDto(190m)));
    }
}
