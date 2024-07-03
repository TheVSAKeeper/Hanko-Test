using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Calabonga.OperationResults;
using HankoTest.Shared.Models;
using HankoTest.Shared.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace HankoTest.Shared;

public class HankoService(IOptions<HankoOptions> options)
{
    private readonly string _hankoJwksUrl = options.Value.JwksUrl;

    public async Task<IList<SecurityKey>> GetSigningKeys()
    {
        HttpClient client = new();
        string jwks = await client.GetStringAsync(_hankoJwksUrl);

        JsonWebKeySet keySet = JsonWebKeySet.Create(jwks);

        return keySet.GetSigningKeys();
    }

    public async Task<ValidatedTokenViewModel> ValidateJwt(string jwt)
    {
        IList<SecurityKey> keys = await GetSigningKeys();

        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKeys = keys,
            RequireSignedTokens = true
        };

        JwtSecurityTokenHandler handler = new();
        handler.InboundClaimTypeMap.Clear();
        ClaimsPrincipal? user = handler.ValidateToken(jwt, parameters, out SecurityToken securityToken);

        JwtSecurityToken payload = handler.ReadJwtToken(jwt);

        return new ValidatedTokenViewModel
        {
            User = user,
            //  Token = securityToken,
            JwtPayload = HankoPayload.FromJwtPayload(payload.Payload)
        };
    }

    public async Task<Operation<ValidatedTokenViewModel, string>> GetUserInfo(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out StringValues jwtValue) == false)
            return Operation.Error("Authorization error");

        string? jwt = jwtValue.FirstOrDefault()?.Split("Bearer ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        if (jwt is null)
            return Operation.Error("JWT in null");

        try
        {
            ValidatedTokenViewModel validateJwt = await ValidateJwt(jwt);
            return Operation.Result(validateJwt);
        }
        catch (Exception e)
        {
            return Operation.Error(e.Message);
        }
    }
}