using Microsoft.AspNetCore.Identity.Data;
using Model.Register;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(UserRegisterRequest registerRequest);

    Task<AuthResponse> LoginAsync(UserLoginRequest userLoginRequest);

}