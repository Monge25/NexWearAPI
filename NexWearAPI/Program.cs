using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexWearAPI.Data;
using NexWearAPI.Services;
using System.Text;
using Microsoft.OpenApi.Models;
using NexWearAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── A05 - Swagger solo en Development + soporte para JWT ─────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NexWear API", Version = "v1" });

    // Botón "Authorize" en Swagger para probar endpoints protegidos
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT. Ejemplo: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Entity Framework + PostgreSQL ────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── A02/A07 - JWT Authentication ─────────────────────────────
var jwtConfig = builder.Configuration.GetSection("Jwt");
var secretKey = jwtConfig["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,   // A07 - Valida que el token no haya expirado
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero  // A07 - Sin margen extra de tiempo al expirar
        };
    });

builder.Services.AddAuthorization();

// ── A05 - CORS restringido ────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Servicios propios ─────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────

// A05 - Swagger solo disponible en entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

// IMPORTANTE: Authentication antes que Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Aplicar migraciones automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();