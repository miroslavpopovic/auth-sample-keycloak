using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Samples.WeatherSummaryApi;
using Samples.WeatherSummaryApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwagger(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority =
            $"{builder.Configuration.GetServiceUri("auth")!.ToString().TrimEnd('/')}/realms/sample";

        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity) return Task.CompletedTask;

                // Split "scope" claim into multiple claims
                // Keycloak returns scopes as space separated list, and not an array as expected by .NET
                var scopeClaims = claimsIdentity.FindFirst("scope");
                if (scopeClaims is null) return Task.CompletedTask;

                claimsIdentity.RemoveClaim(scopeClaims);
                claimsIdentity.AddClaims(scopeClaims.Value.Split(' ').Select(scope => new Claim("scope", scope)));

                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "weather-summary-api");
    });
});


builder.Services
    .AddHttpClient(
        "weather-api-client",
        client => client.BaseAddress = new Uri($"{builder.Configuration.GetServiceUri("weather-api")}weatherforecast"))
    .AcceptAnyServerCertificate();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithOAuth(app.Configuration);
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization("ApiScope");

app.Run();
