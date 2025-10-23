using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.LocalMachine.json", optional: true, reloadOnChange: true);
var jwtKey = builder.Configuration.GetRequiredSection("Jwt:Key");
if (jwtKey.Value is not null && jwtKey.Path is not null && !jwtKey.Value.Equals(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jwtKey.Value))))
{
    builder.Configuration["Jwt:Key"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jwtKey.Value));
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey.Get<byte[]>())
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

builder.Services.AddAuthorization();

builder.Services.AddDbContext<TuringMachinesDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<ICryptoService, AesCryptoService>();
builder.Services.AddScoped<WorkshopItemService>();
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
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
