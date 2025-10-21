using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Development.LocalMachine.json", optional: true, reloadOnChange: true);
var jwtKey = builder.Configuration["Jwt:Key"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<ICryptoService, AesCryptoService>();
builder.Services.AddScoped<WorkshopItemService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
    using (IServiceScope serviceScope = app.Services.CreateScope())
    {
        TuringMachinesDbContext databaseContext = serviceScope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
        databaseContext.Database.Migrate();
    }
}
catch (Exception ex)
{
    throw new Exception("Migração falhou, não foi possível migrar a base de dados.", ex);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
