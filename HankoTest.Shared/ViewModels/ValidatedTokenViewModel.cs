using System.Security.Claims;
using HankoTest.Shared.Models;

namespace HankoTest.Shared.ViewModels;

public record ValidatedTokenViewModel
{
    public ClaimsPrincipal? User { get; init; }

    // public required SecurityToken Token { get; init; }
    public HankoPayload? JwtPayload { get; init; }
    public string? Payload { get; init; }
}