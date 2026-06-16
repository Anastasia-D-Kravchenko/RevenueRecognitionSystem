using RevenueRecognitionSystem.Api.DTOs;

namespace RevenueRecognitionSystem.Api.Services;

public interface IAuthService
{
    Task<TokenDto> LoginAsync(LoginDto dto);
}
