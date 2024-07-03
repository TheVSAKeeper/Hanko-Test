using System.Security.Cryptography;
using System.Text.Json.Serialization;
using HankoTest.Shared;
using HankoTest.Shared.Models;
using HankoTest.Shared.ViewModels;
using Jose;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace HankoTest.FirstApi;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSerilog();

        /*builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));*/

        builder.Services.Configure<HankoOptions>(builder.Configuration.GetSection(nameof(HankoOptions)));
        builder.Services.AddScoped<HankoService>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<AuthorizationHandler>();

        builder.Services.AddHttpClient("auth")
            .AddHttpMessageHandler<AuthorizationHandler>();

        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        IList<SecurityKey> keys = await builder.Services.BuildServiceProvider().GetRequiredService<HankoService>().GetSigningKeys();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration.GetSection(nameof(HankoOptions))[nameof(HankoOptions.JwksUrl)];

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKeys = keys,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine(context.Exception);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "First API", Version = "v1" });

            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();

        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        app.MapGet("/weatherforecast", (HttpContext _) =>
            {
                WeatherForecast[] forecast = Enumerable.Range(1, 5)
                    .Select(index =>
                        new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]))
                    .ToArray();

                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi()
            .RequireAuthorization();

        app.MapGet("/token", async (HttpContext _, string token, [FromServices] HankoService hankoService) => await hankoService.ValidateJwt(token))
            .WithName("ValidateToken")
            .WithOpenApi()
            .RequireAuthorization();

        app.MapGet("/token-jose", ValidateJwtJose)
            .WithName("ValidateTokenJose")
            .WithOpenApi();

        app.MapGet("/weatherforecast-two", ([FromServices] IHttpClientFactory httpClientFactory) =>
            {
                HttpClient client = httpClientFactory.CreateClient("auth");
                return client.GetFromJsonAsync<WeatherForecast[]>("https://localhost:7001/weatherforecast");
            })
            .WithName("GetWeatherForecastFromSecondApi")
            .WithOpenApi();

        app.MapGet("/me", async (HttpContext context, [FromServices] HankoService hankoService) => await hankoService.GetUserInfo(context))
            .WithName("GetUserInfo")
            .WithOpenApi()
            .RequireAuthorization();

        app.MapGet("/me-two", async ([FromServices] IHttpClientFactory httpClientFactory) =>
            {
                HttpClient client = httpClientFactory.CreateClient("auth");
                return await client.GetFromJsonAsync<ValidatedTokenViewModel>("https://localhost:7001/me");
            })
            .WithName("GetUserInfoFromSecondApi")
            .WithOpenApi()
            .RequireAuthorization();

        await app.RunAsync();
    }

    private static async Task<string> ValidateJwtJose(string token)
    {
        HttpClient client = new();

        string keys = await client.GetStringAsync("https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json");

        JwkSet jwks = JwkSet.FromJson(keys, JWT.DefaultSettings.JsonMapper);

        string? jwt = JWT.Decode(token, jwks, JwsAlgorithm.RS256);

        return jwt;
    }

    #region GetSigningKeys

    public record JsonWebKeySet(
        JsonWebKey[] keys
    );

    public record JsonWebKey(
        string alg,
        string e,
        string kid,
        string kty,
        string n,
        string use
    );

    private static async Task<List<SecurityKey>> GetSigningKeys()
    {
        HttpClient client = new();
        JsonWebKeySet? keySet = await client.GetFromJsonAsync<JsonWebKeySet>("https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json");

        if (keySet is null)
            return [];

        JsonWebKey[] jsonWebKeys = keySet.keys;
        List<SecurityKey> keys = [];

        foreach (JsonWebKey webKey in jsonWebKeys)
        {
            byte[]? e = Base64Url.Decode(webKey.e);
            byte[]? n = Base64Url.Decode(webKey.n);

            RSAParameters parameters = new()
            {
                Exponent = e,
                Modulus = n
            };

            RsaSecurityKey key = new(parameters)
            {
                KeyId = webKey.kid
            };

            keys.Add(key);
        }

        return keys;
    }

    #endregion
}