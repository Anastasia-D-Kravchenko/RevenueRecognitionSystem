using System.Globalization;
using System.Net;
using System.Text;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Services;
using Xunit;

namespace RevenueRecognitionSystem.Tests;

public class RevenueServiceTests
{
    private static NbpExchangeRateProvider FakeRate(decimal mid)
    {
        var midStr = mid.ToString(CultureInfo.InvariantCulture);
        var json = "{\"code\":\"EUR\",\"rates\":[{\"no\":\"1\",\"effectiveDate\":\"2026-06-15\",\"mid\":" + midStr + "}]}";
        var http = new HttpClient(new FakeHttpHandler(json));
        return new NbpExchangeRateProvider(http);
    }

    private static NbpExchangeRateProvider NoCallRate()
    {
        return new NbpExchangeRateProvider(new HttpClient(new FakeHttpHandler("{}")));
    }

    [Fact]
    public async Task GetCurrent_SignedContractsPlusSubPayments()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Contracts.AddRange(
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
                SupportYears = 1, TotalPrice = 5000m, Status = ContractStatus.Signed
            },
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10),
                SupportYears = 1, TotalPrice = 2000m, Status = ContractStatus.Pending
            });
        var sub = new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 100m,
            StartDate = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        sub.Payments.Add(new SubscriptionPayment
        {
            Amount = 100m, PaidAt = DateTime.UtcNow,
            PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow.AddMonths(1)
        });
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetCurrentAsync(null, "PLN");

        Assert.Equal(5100m, result.Amount);
        Assert.Equal("PLN", result.Currency);
        Assert.Equal("Current", result.Scope);
    }

    [Fact]
    public async Task GetCurrent_ConvertsToEur()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
            SupportYears = 1, TotalPrice = 4000m, Status = ContractStatus.Signed
        });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, FakeRate(4m));
        var result = await service.GetCurrentAsync(null, "EUR");
        Assert.Equal(1000m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public async Task GetCurrent_FilterBySoftware()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Softwares.Add(new Software
        {
            SoftwareId = 2, Name = "Other", Description = "d",
            CurrentVersion = "1.0", YearlyLicensePrice = 1000m, SoftwareCategoryId = 1
        });
        db.Contracts.AddRange(
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
                SupportYears = 1, TotalPrice = 5000m, Status = ContractStatus.Signed
            },
            new Contract
            {
                CustomerId = 1, SoftwareId = 2, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
                SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Signed
            });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetCurrentAsync(1, "PLN");
        Assert.Equal(5000m, result.Amount);
        Assert.Equal(1, result.SoftwareId);
    }

    [Fact]
    public async Task GetCurrent_NoData_ReturnsZero()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetCurrentAsync(null, "PLN");
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetPredicted_IncludesSignedAndStillLivePending()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Contracts.AddRange(
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
                SupportYears = 1, TotalPrice = 5000m, Status = ContractStatus.Signed
            },
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
                SupportYears = 1, TotalPrice = 2000m, Status = ContractStatus.Pending
            });
        db.Subscriptions.Add(new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 3, RenewalPrice = 300m,
            StartDate = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(3),
            Status = SubscriptionStatus.Active
        });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetPredictedAsync(null, "PLN");
        Assert.Equal(8200m, result.Amount);
    }

    [Fact]
    public async Task GetPredicted_SkipsZombiePendingContracts()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Contracts.AddRange(
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddMonths(-6), EndDate = DateTime.UtcNow.AddMonths(-5),
                SupportYears = 1, TotalPrice = 9999m, Status = ContractStatus.Pending
            },
            new Contract
            {
                CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
                StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
                SupportYears = 1, TotalPrice = 100m, Status = ContractStatus.Pending
            });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetPredictedAsync(null, "PLN");
        Assert.Equal(100m, result.Amount);
    }

    [Fact]
    public async Task GetPredicted_SkipsOverdueSubscriptions()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Subscriptions.AddRange(
            new Subscription
            {
                Name = "alive", CustomerId = 1, SoftwareId = 1,
                RenewalPeriodMonths = 1, RenewalPrice = 100m,
                StartDate = DateTime.UtcNow.AddDays(-15),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(15),
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                Name = "abandoned", CustomerId = 1, SoftwareId = 1,
                RenewalPeriodMonths = 1, RenewalPrice = 9999m,
                StartDate = DateTime.UtcNow.AddMonths(-6),
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(-5),
                Status = SubscriptionStatus.Active
            });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetPredictedAsync(null, "PLN");
        Assert.Equal(1200m, result.Amount);
    }

    [Fact]
    public async Task GetPredicted_LongPeriodSub_ForecastsZero()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Subscriptions.Add(new Subscription
        {
            Name = "biennial", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 24, RenewalPrice = 10000m,
            StartDate = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(24),
            Status = SubscriptionStatus.Active
        });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetPredictedAsync(null, "PLN");
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetPredicted_CancelledContract_Excluded()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 9999m, Status = ContractStatus.Cancelled
        });
        await db.SaveChangesAsync();
        var service = new RevenueService(db, NoCallRate());
        var result = await service.GetPredictedAsync(null, "PLN");
        Assert.Equal(0m, result.Amount);
    }
}

internal class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _response;
    public FakeHttpHandler(string response) => _response = response;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_response, Encoding.UTF8, "application/json")
        });
    }
}
