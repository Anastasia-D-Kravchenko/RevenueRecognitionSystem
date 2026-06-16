namespace RevenueRecognitionSystem.Api.Entities;

public enum ContractStatus
{
    Pending = 0,
    Signed = 1,
    Cancelled = 2
}

public class Contract
{
    public int ContractId { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public string SoftwareVersion { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int SupportYears { get; set; }
    public decimal TotalPrice { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Pending;
    public DateTime? SignedAt { get; set; }

    public ICollection<ContractPayment> Payments { get; set; } = new List<ContractPayment>();
}
