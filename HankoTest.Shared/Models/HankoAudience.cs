namespace HankoTest.Shared.Models;

public class HankoAudience
{
    /// <summary>
    ///     The audience for which the JWT was created. It specifies the intended recipient or system that should accept this
    ///     JWT.
    ///     When using Hanko Cloud, the aud will be your app URL.
    /// </summary>
    public required List<string> AudienceValues { get; init; }
}