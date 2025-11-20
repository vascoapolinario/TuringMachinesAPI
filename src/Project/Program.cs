using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Hubs;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.LocalMachine.json", optional: true, reloadOnChange: true);
var jwtKey = builder.Configuration.GetRequiredSection("Jwt:Key");
if (jwtKey.Value is not null && jwtKey.Path is not null && !jwtKey.Value.Equals(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jwtKey.Value))))
{
    builder.Configuration["Jwt:Key"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jwtKey.Value));
}

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey.Get<byte[]>()),
            NameClaimType = "sub",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/lobby")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var services = context.HttpContext.RequestServices;
                var playerService = services.GetRequiredService<PlayerService>();

                var (id, username, role) = playerService.GetClaimsFromUser(context.Principal!);

                if (string.IsNullOrEmpty(id) ||
                    string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(role))
                {
                    context.Fail("Invalid token claims.");
                    return Task.CompletedTask;
                }

                var exists = playerService.PlayerExistsAsIs(id, username, role);
                if (!exists)
                {
                    context.Fail("User does not exist or claims are outdated.");
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userIdentifier = context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(userIdentifier, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "You are sending too many requests. Please slow down.",
            cancellationToken: token);
    };
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Turing Machines API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<TuringMachinesDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();
builder.Services.AddSingleton<ICryptoService, AesCryptoService>();
builder.Services.AddSingleton<DiscordWebhookService>();
builder.Services.AddScoped<WorkshopItemService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<PlayerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    throw new Exception("Migração falhou, não foi possível migrar a base de dados.", ex);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseApplicationInformation();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LobbyHub>("/hubs/lobby");

app.Run();
