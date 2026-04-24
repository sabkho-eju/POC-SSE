using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PocSSE.Backend.WebApi.Services;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;
using PocSSE.Backend.WebApi.Infra.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        if(string.IsNullOrEmpty(secretKey)) throw new InvalidConfigurationException("JWT SecretKey not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "POC-SSE-Backend",
            ValidAudience = jwtSettings["Audience"] ?? "POC-SSE-Frontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        //Pour SSE : accepter le token depuis query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Vérification exacte du path (case-sensitive)
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.Equals("/api/jobprocessing/job-notification-stream", StringComparison.Ordinal) || 
                     path.Equals("/api/messaging/messaging-notification-stream", StringComparison.Ordinal)))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Enregistrer le service d'authentification
builder.Services.AddSingleton<AuthenticationService>();

builder.Services.AddSingleton<NotificationQueue>();
builder.Services.AddHostedService<JobProcessorWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();  
app.UseAuthorization();

app.MapControllers();

app.Run();
