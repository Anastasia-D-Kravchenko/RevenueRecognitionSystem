using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Exceptions;

namespace RevenueRecognitionSystem.Api.Services;

public class ContractService : IContractService
{
    private const decimal AdditionalSupportYearCost = 1000m;
    private const int MinContractDays = 3;
    private const int MaxContractDays = 30;

    private readonly AppDbContext _db;
    private readonly DiscountPolicy _discounts;

    public ContractService(AppDbContext db, DiscountPolicy discounts)
    {
        _db = db;
        _discounts = discounts;
    }

    public async Task<ContractDto> CreateAsync(CreateContractDto dto)
    {
        var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days;
        if (totalDays < MinContractDays || totalDays > MaxContractDays)
            throw new BadRequestException(
                $"Contract length must be between {MinContractDays} and {MaxContractDays} days.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId);
        if (customer is null)
            throw new NotFoundException($"Customer with id={dto.CustomerId} was not found.");
        if (customer is IndividualCustomer { IsDeleted: true })
            throw new ConflictException("Customer has been deleted.");

        var software = await _db.Softwares.FirstOrDefaultAsync(s => s.SoftwareId == dto.SoftwareId);
        if (software is null)
            throw new NotFoundException($"Software with id={dto.SoftwareId} was not found.");

        var now = DateTime.UtcNow;

        var hasActiveContract = await _db.Contracts.AnyAsync(c =>
            c.CustomerId == dto.CustomerId
            && c.SoftwareId == dto.SoftwareId
            && c.Status == ContractStatus.Pending
            && c.EndDate >= now);

        var hasActiveSubscription = await _db.Subscriptions.AnyAsync(s =>
            s.CustomerId == dto.CustomerId
            && s.SoftwareId == dto.SoftwareId
            && s.Status == SubscriptionStatus.Active);

        if (hasActiveContract || hasActiveSubscription)
            throw new ConflictException(
                "Customer already has an active contract or subscription for this product.");

        var isReturning =
            await _db.Contracts.AnyAsync(c => c.CustomerId == dto.CustomerId && c.Status == ContractStatus.Signed)
            || await _db.Subscriptions.AnyAsync(s => s.CustomerId == dto.CustomerId);

        var discounts = await _db.Discounts.ToListAsync();
        var promo = _discounts.PickHighestActive(discounts, DiscountOffer.Contract, now);
        var returning = _discounts.ReturningCustomerBonus(isReturning);
        var totalDiscountPercent = Math.Min(promo + returning, 100m);

        var basePrice = software.YearlyLicensePrice;
        var supportCost = dto.AdditionalSupportYears * AdditionalSupportYearCost;
        var discounted = basePrice * (1m - totalDiscountPercent / 100m);
        var totalPrice = Math.Round(discounted + supportCost, 2);

        var contract = new Contract
        {
            CustomerId = dto.CustomerId,
            SoftwareId = dto.SoftwareId,
            SoftwareVersion = dto.SoftwareVersion,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SupportYears = 1 + dto.AdditionalSupportYears,
            TotalPrice = totalPrice,
            Status = ContractStatus.Pending
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        return ToDto(contract, 0m);
    }

    public async Task DeleteAsync(int contractId)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractId == contractId);
        if (contract is null)
            throw new NotFoundException($"Contract with id={contractId} was not found.");
        if (contract.Status == ContractStatus.Signed)
            throw new ConflictException("Cannot delete a signed contract.");

        _db.Contracts.Remove(contract);
        await _db.SaveChangesAsync();
    }

    public async Task<ContractDto> GetAsync(int contractId)
    {
        var contract = await _db.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);
        if (contract is null)
            throw new NotFoundException($"Contract with id={contractId} was not found.");

        var amountPaid = contract.Payments.Where(p => !p.Refunded).Sum(p => p.Amount);
        return ToDto(contract, amountPaid);
    }

    public async Task<ContractDto> AddPaymentAsync(int contractId, AddContractPaymentDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var contract = await _db.Contracts
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);
        if (contract is null)
            throw new NotFoundException($"Contract with id={contractId} was not found.");

        var now = DateTime.UtcNow;

        if (contract.Status == ContractStatus.Cancelled)
            throw new ConflictException("Contract is cancelled.");
        if (contract.Status == ContractStatus.Signed)
            throw new ConflictException("Contract is already fully paid.");

        if (now < contract.StartDate)
            throw new ConflictException(
                $"Contract offer is not open yet (opens on {contract.StartDate:yyyy-MM-dd}).");

        if (now > contract.EndDate)
        {
            foreach (var p in contract.Payments.Where(p => !p.Refunded))
                p.Refunded = true;
            contract.Status = ContractStatus.Cancelled;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            throw new ConflictException(
                "Payment date is past the contract end date. Previous payments refunded.");
        }

        var alreadyPaid = contract.Payments.Where(p => !p.Refunded).Sum(p => p.Amount);
        var remaining = contract.TotalPrice - alreadyPaid;
        if (dto.Amount > remaining)
            throw new BadRequestException(
                $"Payment exceeds remaining amount. Remaining: {remaining}.");

        _db.ContractPayments.Add(new ContractPayment
        {
            ContractId = contract.ContractId,
            Amount = dto.Amount,
            PaidAt = now,
            Refunded = false
        });

        var totalAfter = alreadyPaid + dto.Amount;
        if (totalAfter == contract.TotalPrice)
        {
            contract.Status = ContractStatus.Signed;
            contract.SignedAt = now;
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return ToDto(contract, totalAfter);
    }

    private static ContractDto ToDto(Contract c, decimal amountPaid) => new(
        c.ContractId, c.CustomerId, c.SoftwareId, c.SoftwareVersion,
        c.StartDate, c.EndDate, c.SupportYears, c.TotalPrice,
        amountPaid, c.Status.ToString(), c.SignedAt);
}
