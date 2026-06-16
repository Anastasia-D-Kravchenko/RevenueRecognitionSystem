using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Services;

public class DiscountPolicy
{
    public decimal PickHighestActive(IEnumerable<Discount> discounts, DiscountOffer offer, DateTime at)
    {
        return discounts
            .Where(d => d.Offer == offer && d.StartDate <= at && d.EndDate >= at)
            .Select(d => d.Value)
            .DefaultIfEmpty(0m)
            .Max();
    }

    public decimal ReturningCustomerBonus(bool isReturning) => isReturning ? 5m : 0m;
}
