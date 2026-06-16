using RevenueRecognitionSystem.Api.DTOs;

namespace RevenueRecognitionSystem.Api.Services;

public interface IContractService
{
    Task<ContractDto> CreateAsync(CreateContractDto dto);
    Task DeleteAsync(int contractId);
    Task<ContractDto> GetAsync(int contractId);
    Task<ContractDto> AddPaymentAsync(int contractId, AddContractPaymentDto dto);
}
