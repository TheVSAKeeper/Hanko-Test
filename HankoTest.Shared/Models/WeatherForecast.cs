namespace HankoTest.Shared.Models;

public record WeatherForecast(string Id, DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}