using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Exceptions;
using RevenueRecognitionSystem.Api.Services;
using Xunit;

namespace RevenueRecognitionSystem.Tests;

public class ContractServiceTests
{
    private static ContractService NewService(out AppDbContext db)
    {
        db = TestHelpers.NewDb();
        return new ContractService(db, new DiscountPolicy());
    }

    [Fact]
    public async Task Create_LengthBelowMin_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), 0);
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_LengthAboveMax_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(45), 0);
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ExactlyThreeDays_Accepted()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(3), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task Create_ExactlyThirtyDays_Accepted()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task Create_CustomerNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(999, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_SoftwareNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 999, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_DeletedCustomer_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var c = db.IndividualCustomers.Single();
        c.IsDeleted = true;
        await db.SaveChangesAsync();
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_BasePriceWithSupport_TotalIsBasePlusSupportCost()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 2);
        var result = await service.CreateAsync(dto);
        Assert.Equal(7000m, result.TotalPrice);
        Assert.Equal(3, result.SupportYears);
    }

    [Fact]
    public async Task Create_NoAdditionalSupport_OneYearOfUpdates()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal(1, result.SupportYears);
        Assert.Equal(5000m, result.TotalPrice);
    }

    [Fact]
    public async Task Create_ReturningCustomerFromContract_AppliesFivePercent()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "old",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow.AddYears(-1).AddDays(10),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Signed
        });
        await db.SaveChangesAsync();
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal(4750m, result.TotalPrice);
    }

    [Fact]
    public async Task Create_ReturningCustomerFromSubscription_AppliesFivePercent()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Subscriptions.Add(new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 1m,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddYears(-1).AddMonths(1),
            Status = SubscriptionStatus.Cancelled
        });
        await db.SaveChangesAsync();
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal(4750m, result.TotalPrice);
    }

    [Fact]
    public async Task Create_ActiveContractExistsSameSoftware_ThrowsConflict()
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
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ActiveSubscriptionSameSoftware_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Subscriptions.Add(new Subscription
        {
            Name = "S", CustomerId = 1, SoftwareId = 1,
            RenewalPeriodMonths = 1, RenewalPrice = 100m,
            StartDate = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        });
        await db.SaveChangesAsync();
        var dto = new CreateContractDto(1, 1, "2026.1",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_DifferentSoftware_Allowed()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        db.Softwares.Add(new Software
        {
            SoftwareId = 2, Name = "Other", Description = "d",
            CurrentVersion = "1.0", YearlyLicensePrice = 1000m, SoftwareCategoryId = 1
        });
        db.Contracts.Add(new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Pending
        });
        await db.SaveChangesAsync();
        var dto = new CreateContractDto(1, 2, "1.0",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), 0);
        var result = await service.CreateAsync(dto);
        Assert.Equal(2, result.SoftwareId);
    }

    [Fact]
    public async Task AddPayment_Full_SignsContract()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Pending
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        var result = await service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(1000m));
        Assert.Equal("Signed", result.Status);
        Assert.Equal(1000m, result.AmountPaid);
        Assert.NotNull(db.Contracts.Single().SignedAt);
    }

    [Fact]
    public async Task AddPayment_PartialThenFull_SignsOnFinal()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Pending
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        var partial = await service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(400m));
        Assert.Equal("Pending", partial.Status);
        Assert.Equal(400m, partial.AmountPaid);
        var final = await service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(600m));
        Assert.Equal("Signed", final.Status);
        Assert.Equal(1000m, final.AmountPaid);
    }

    [Fact]
    public async Task AddPayment_Overpayment_ThrowsBadRequest()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Pending
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(2000m)));
    }

    [Fact]
    public async Task AddPayment_AfterEndDate_RefundsAndCancels()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(-1),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Pending
        };
        contract.Payments.Add(new ContractPayment
        {
            Amount = 400m, PaidAt = DateTime.UtcNow.AddDays(-15), Refunded = false
        });
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(600m)));
        var reloaded = db.Contracts.Single();
        Assert.Equal(ContractStatus.Cancelled, reloaded.Status);
        Assert.All(reloaded.Payments, p => Assert.True(p.Refunded));
    }

    [Fact]
    public async Task AddPayment_BeforeStartDate_ThrowsConflict_NoPaymentStored()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(5), EndDate = DateTime.UtcNow.AddDays(15),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Pending
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(1000m)));
        Assert.Equal(ContractStatus.Pending, db.Contracts.Single().Status);
        Assert.Empty(db.ContractPayments);
    }

    [Fact]
    public async Task AddPayment_AlreadySigned_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1000m,
            Status = ContractStatus.Signed, SignedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(100m)));
    }

    [Fact]
    public async Task AddPayment_AlreadyCancelled_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1000m, Status = ContractStatus.Cancelled
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddPaymentAsync(contract.ContractId, new AddContractPaymentDto(100m)));
    }

    [Fact]
    public async Task AddPayment_ContractNotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddPaymentAsync(999, new AddContractPaymentDto(100m)));
    }

    [Fact]
    public async Task Delete_SignedContract_ThrowsConflict()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Signed
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(contract.ContractId));
    }

    [Fact]
    public async Task Delete_PendingContract_Removed()
    {
        var service = NewService(out var db);
        await TestHelpers.SeedAsync(db);
        var contract = new Contract
        {
            CustomerId = 1, SoftwareId = 1, SoftwareVersion = "v",
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10),
            SupportYears = 1, TotalPrice = 1m, Status = ContractStatus.Pending
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        await service.DeleteAsync(contract.ContractId);
        Assert.Empty(db.Contracts);
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsNotFound()
    {
        var service = NewService(out var db);
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
    }
}
