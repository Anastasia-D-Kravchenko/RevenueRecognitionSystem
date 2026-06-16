using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Services;
using Xunit;

namespace RevenueRecognitionSystem.Tests;

public class DiscountPolicyTests
{
    private readonly DiscountPolicy _policy = new();

    [Fact]
    public void PickHighestActive_EmptyList_ReturnsZero()
    {
        var result = _policy.PickHighestActive(new List<Discount>(), DiscountOffer.Contract, DateTime.UtcNow);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void PickHighestActive_NoMatchingOffer_ReturnsZero()
    {
        var discounts = new[]
        {
            new Discount
            {
                Offer = DiscountOffer.Subscription, Value = 20m,
                StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31)
            }
        };
        var result = _policy.PickHighestActive(discounts, DiscountOffer.Contract, new DateTime(2026, 6, 1));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void PickHighestActive_OutsideDateRange_ReturnsZero()
    {
        var discounts = new[]
        {
            new Discount
            {
                Offer = DiscountOffer.Contract, Value = 20m,
                StartDate = new DateTime(2027, 1, 1), EndDate = new DateTime(2027, 12, 31)
            }
        };
        var result = _policy.PickHighestActive(discounts, DiscountOffer.Contract, new DateTime(2026, 6, 1));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void PickHighestActive_MultipleActive_ReturnsMaxValue()
    {
        var discounts = new[]
        {
            new Discount
            {
                Offer = DiscountOffer.Contract, Value = 10m,
                StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31)
            },
            new Discount
            {
                Offer = DiscountOffer.Contract, Value = 25m,
                StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 6, 30)
            },
            new Discount
            {
                Offer = DiscountOffer.Contract, Value = 15m,
                StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 6, 15)
            }
        };
        var result = _policy.PickHighestActive(discounts, DiscountOffer.Contract, new DateTime(2026, 6, 10));
        Assert.Equal(25m, result);
    }

    [Fact]
    public void PickHighestActive_BoundaryStartDate_Included()
    {
        var discounts = new[]
        {
            new Discount
            {
                Offer = DiscountOffer.Contract, Value = 10m,
                StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 6, 30)
            }
        };
        var result = _policy.PickHighestActive(discounts, DiscountOffer.Contract, new DateTime(2026, 6, 1));
        Assert.Equal(10m, result);
    }

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, 0)]
    public void ReturningCustomerBonus_ReturnsExpected(bool isReturning, decimal expected)
    {
        Assert.Equal(expected, _policy.ReturningCustomerBonus(isReturning));
    }
}
