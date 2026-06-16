namespace RevenueRecognitionSystem.Api.Entities;

public enum SubscriptionStatus
{
    Active = 0,
    Cancelled = 1
}

public class Subscription
{
    public int SubscriptionId { get; set; }
    public string Name { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public int RenewalPeriodMonths { get; set; }
    public decimal RenewalPrice { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
}
