namespace RevenueRecognitionSystem.Api.Entities;

public enum DiscountOffer
{
    Contract = 0,
    Subscription = 1
}

public class Discount
{
    public int DiscountId { get; set; }
    public string Name { get; set; } = null!;
    public DiscountOffer Offer { get; set; }
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
