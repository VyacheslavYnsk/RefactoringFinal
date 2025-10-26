public class TokenRevocationService : ITokenRevocationService
{
    private readonly List<RevokedToken> _revokedTokens = new();

    public async Task<bool> RevokeTokenAsync(string token)
    {
        if (await IsTokenRevokedAsync(token))
        {
            return false;
        }
        
        _revokedTokens.Add(new RevokedToken { Token = token, RevokedAt = DateTime.UtcNow });
        return true; 
    }

    public async Task<bool> IsTokenRevokedAsync(string token)
    {
       
        return _revokedTokens.Any(rt => rt.Token == token);
    }
}