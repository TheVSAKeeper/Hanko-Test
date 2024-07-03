using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Newtonsoft.Json;

namespace HankoTest.Shared.Models;

public class HankoPayload
{
    /// <summary>
    ///     The audience for which the JWT was created.
    /// </summary>
    public required HankoAudience Audience { get; init; }

    /// <summary>
    ///     An object containing information about the user's email address.
    /// </summary>
    public required HankoUserEmail Email { get; init; }

    /// <summary>
    ///     The timestamp indicating when the JWT will expire.
    /// </summary>
    public required DateTime ExpirationTime { get; init; }

    /// <summary>
    ///     The timestamp indicating when the JWT was created.
    /// </summary>
    public required DateTime IssuedAt { get; init; }

    /// <summary>
    ///     The user ID.
    /// </summary>
    public required string Subject { get; init; }

    public static HankoPayload FromJwtPayload(JwtPayload jwtPayload)
    {
        JsonElement emailJson = (JsonElement)jwtPayload["email"];
        HankoUserEmail emailObject = JsonConvert.DeserializeObject<HankoUserEmail>(emailJson.ToString())!;

        return new HankoPayload
        {
            Audience = new HankoAudience
            {
                AudienceValues = (List<string>)jwtPayload["aud"]
            },
            Email = emailObject,
            ExpirationTime = DateTimeOffset.FromUnixTimeSeconds((long)jwtPayload["exp"]).DateTime,
            IssuedAt = DateTimeOffset.FromUnixTimeSeconds((long)jwtPayload["iat"]).DateTime,
            Subject = (string)jwtPayload["sub"]
        };
    }
}