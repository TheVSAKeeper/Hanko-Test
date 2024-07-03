namespace HankoTest.Shared;

public record HankoOptions
{
    public HankoOptions()
    {
    }

    public HankoOptions(string apiUrl, string? jwksUrl = null)
    {
        ApiUrl = apiUrl;
        JwksUrl = jwksUrl ?? $"{apiUrl}/.well-known/jwks.json";
    }

    public string ApiUrl { get; init; }
    public string JwksUrl { get; init; }
}