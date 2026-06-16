using System.ComponentModel.DataAnnotations;

namespace RevenueRecognitionSystem.Api.DTOs;

public record LoginDto(
    [Required] [MaxLength(50)] string Login,
    [Required] [MaxLength(100)] string Password);

public record CreateIndividualCustomerDto(
    [Required] [MaxLength(50)] string FirstName,
    [Required] [MaxLength(100)] string LastName,
    [Required] [RegularExpression("^[0-9]{11}$", ErrorMessage = "PESEL must be exactly 11 digits.")]
    string Pesel,
    [Required] [MaxLength(200)] string Address,
    [Required] [EmailAddress] [MaxLength(100)] string Email,
    [Required] [Phone] [MaxLength(30)] string PhoneNumber);

public record CreateCompanyCustomerDto(
    [Required] [MaxLength(150)] string CompanyName,
    [Required] [RegularExpression("^[0-9]{10}$", ErrorMessage = "KRS must be exactly 10 digits.")]
    string Krs,
    [Required] [MaxLength(200)] string Address,
    [Required] [EmailAddress] [MaxLength(100)] string Email,
    [Required] [Phone] [MaxLength(30)] string PhoneNumber);

public record UpdateIndividualCustomerDto(
    [Required] [MaxLength(50)] string FirstName,
    [Required] [MaxLength(100)] string LastName,
    [Required] [MaxLength(200)] string Address,
    [Required] [EmailAddress] [MaxLength(100)] string Email,
    [Required] [Phone] [MaxLength(30)] string PhoneNumber);

public record UpdateCompanyCustomerDto(
    [Required] [MaxLength(150)] string CompanyName,
    [Required] [MaxLength(200)] string Address,
    [Required] [EmailAddress] [MaxLength(100)] string Email,
    [Required] [Phone] [MaxLength(30)] string PhoneNumber);

public record CreateContractDto(
    [Range(1, int.MaxValue)] int CustomerId,
    [Range(1, int.MaxValue)] int SoftwareId,
    [Required] [MaxLength(20)] string SoftwareVersion,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Range(0, 3, ErrorMessage = "Additional support can be 0, 1, 2 or 3 years.")]
    int AdditionalSupportYears);

public record AddContractPaymentDto(
    [Range(0.01, double.MaxValue)] decimal Amount);

public record CreateSubscriptionDto(
    [Range(1, int.MaxValue)] int CustomerId,
    [Range(1, int.MaxValue)] int SoftwareId,
    [Required] [MaxLength(100)] string Name,
    [Range(1, 24, ErrorMessage = "Renewal period must be between 1 and 24 months.")]
    int RenewalPeriodMonths,
    [Range(0.01, double.MaxValue)] decimal RenewalPrice);

public record PaySubscriptionDto(
    [Range(0.01, double.MaxValue)] decimal Amount);
