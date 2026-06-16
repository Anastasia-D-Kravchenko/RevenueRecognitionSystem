namespace RevenueRecognitionSystem.Api.Entities;

public class Software
{
    public int SoftwareId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string CurrentVersion { get; set; } = null!;
    public decimal YearlyLicensePrice { get; set; }

    public int SoftwareCategoryId { get; set; }
    public SoftwareCategory Category { get; set; } = null!;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
