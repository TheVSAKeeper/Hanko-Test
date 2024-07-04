using System.Text.Json.Serialization;
using HankoTest.Shared;
using HankoTest.Shared.Models;
using HankoTest.Shared.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace HankoTest.SecondApi;

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

                options.RequireHttpsMetadata = false;
                
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
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Second API", Version = "v1" });

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

        string[] summaries = ["Мороз", "Прохладно", "Прохладновато", "Прохладно", "Умеренно", "Тепло", "Тепло", "Жарко", "Очень жарко", "Огненно жарко"];

        app.MapGet("/weatherforecast", (HttpContext _) =>
            {
                WeatherForecast[] forecast = Enumerable.Range(1, 5)
                    .Select(index =>
                        new WeatherForecast(Guid.NewGuid().ToString(), DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
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

        app.MapGet("/me", async (HttpContext context, [FromServices] ILogger<ValidatedTokenViewModel> logger, [FromServices] HankoService hankoService) =>
            {
                //Operation<ValidatedTokenViewModel, string> result = await ;

                /*if (result)
                    logger.LogDebug("GetUserInfo: {Result}", result);
                else
                    logger.LogError("GetUserInfo: {Error}", result.Error);

                if (result)
                    Log.Debug("GetUserInfo: {Result}", result);
                else
                   Log.Error("GetUserInfo: {Error}", result.Error);*/

                return (await hankoService.GetUserInfo(context)).Result;
            })
            .WithName("GetUserInfo")
            .WithOpenApi()
            .RequireAuthorization();

        await app.RunAsync();
    }
}