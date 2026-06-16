namespace RevenueRecognitionSystem.Api.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public string Login { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;
}
