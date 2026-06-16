using RevenueRecognitionSystem.Api.DTOs;

namespace RevenueRecognitionSystem.Api.Services;

public interface ICustomerService
{
    Task<int> AddIndividualAsync(CreateIndividualCustomerDto dto);
    Task<int> AddCompanyAsync(CreateCompanyCustomerDto dto);
    Task UpdateIndividualAsync(int id, UpdateIndividualCustomerDto dto);
    Task UpdateCompanyAsync(int id, UpdateCompanyCustomerDto dto);
    Task SoftDeleteIndividualAsync(int id);
    Task<IEnumerable<CustomerListItemDto>> ListAsync();
}
