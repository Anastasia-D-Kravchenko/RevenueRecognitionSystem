namespace RevenueRecognitionSystem.Api.Entities;

public class IndividualCustomer : Customer
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Pesel { get; set; } = null!;
    public bool IsDeleted { get; set; }
}
