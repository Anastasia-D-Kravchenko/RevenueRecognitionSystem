namespace RevenueRecognitionSystem.Api.Entities;

public class ContractPayment
{
    public int ContractPaymentId { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public bool Refunded { get; set; }
}
