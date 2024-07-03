using Newtonsoft.Json;

namespace HankoTest.Shared.Models;

public class HankoUserEmail
{
    /// <summary>
    ///     The current primary email address of the user.
    /// </summary>
    [JsonProperty("address")]
    public required string Address { get; init; }

    /// <summary>
    ///     A boolean field indicating whether the email address is the primary email.
    ///     Currently, this field is redundant because only the primary email is included in the JWT.
    /// </summary>
    [JsonProperty("is_primary")]
    public required bool IsPrimary { get; init; }

    /// <summary>
    ///     A boolean field indicating whether the email address has been verified.
    /// </summary>
    [JsonProperty("is_verified")]
    public required bool IsVerified { get; init; }
}