using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PocSSE.Backend.WebApi.Infra;
using PocSSE.Backend.WebApi.Services;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;

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

        // Pour SSE : accepter le token depuis query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/sse"))
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

builder.Services.AddSingleton<BackgroundJobQueue>();
builder.Services.AddSingleton<JobNotificationService>();
builder.Services.AddHostedService<JobProcessorWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// EventSource ne supporte pas les headers personnalisés, donc le token JWT est passé en query string
// le backend doit accepter le token de 2 manières :
// Header Authorization : Authorization: Bearer (endpoint classic)
// Query string (endpoint SSE)
app.Use(async (context, next) =>
{
    // Si le token n'est pas dans le header, chercher dans query string
    if (context.Request.Path.StartsWithSegments("/api/serviceeventnotification/ssestream"))
    {
        var token = context.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(token) &&
            !context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Request.Headers.Append("Authorization", $"Bearer {token}");
        }
    }
    await next();
});

// HTTPS Redirection - Désactivé en développement pour éviter les warnings
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();  
app.UseAuthorization();

app.MapControllers();

app.Run();
