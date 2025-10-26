public interface ITokenRevocationService
{
    Task<bool> RevokeTokenAsync(string token);
    Task<bool> IsTokenRevokedAsync(string token);
}