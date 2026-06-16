namespace RevenueRecognitionSystem.Api.Entities;

public class SubscriptionPayment
{
    public int SubscriptionPaymentId { get; set; }
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

