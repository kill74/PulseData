using PulseData.API.Configuration;
using PulseData.API.Middleware;
using PulseData.Core.Interfaces;
using PulseData.Infrastructure.Data;
using PulseData.Infrastructure.Repositories;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from appsettings files
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuration Options (bind from appsettings)
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LoggingOptions>(builder.Configuration.GetSection(LoggingOptions.SectionName));

// Swagger/OpenAPI with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "PulseData API",
        Version = "v1",
        Description = "E-Commerce Analytics Platform — exposes sales, product, and customer data.",
        Contact = new()
        {
            Name = "PulseData Team",
            Url = new Uri("https://github.com/kill74/PulseData")
        }
    });

    // Add JWT authorization to Swagger
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token in the format: Bearer {token}"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });

    // Include XML comments from source code
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Database
builder.Services.AddSingleton<DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// CORS — configuration-driven, defaults to secure setup
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment() && corsOptions.AllowedOrigins.Length == 0)
    {
        // Development: allow localhost
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5174")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    }
    else if (corsOptions.AllowedOrigins.Length > 0)
    {
        // Production: use configured origins
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                  .WithMethods(corsOptions.AllowedMethods)
                  .WithHeaders(corsOptions.AllowedHeaders)
                  .WithExposedHeaders("Content-Disposition")
                  .SetPreflightMaxAge(TimeSpan.FromSeconds(corsOptions.MaxAge));

            if (corsOptions.AllowCredentials)
                policy.AllowCredentials();
        });
    }
    else
    {
        // Fallback: restrict to HTTPS origins only (very safe)
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("https://localhost")
                  .AllowAnyHeader()
                  .WithMethods("GET", "POST", "PUT", "DELETE"));
    }
});

// Authentication (JWT) — optional; add only if issuer is configured
if (!string.IsNullOrEmpty(builder.Configuration.GetSection(JwtOptions.SectionName)["Issuer"]))
{
    builder.Services
        .AddAuthentication("Bearer")
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration[$"{JwtOptions.SectionName}:Issuer"];
            options.Audience = builder.Configuration[$"{JwtOptions.SectionName}:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });

    builder.Services.AddAuthorization();
}

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------

var app = builder.Build();

// Exception handling (MUST be first!)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Request logging
if (builder.Environment.IsDevelopment())
    app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PulseData API v1");
        options.RoutePrefix = string.Empty;  // Swagger at root
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Authentication & Authorization (if enabled)
if (!string.IsNullOrEmpty(builder.Configuration.GetSection(JwtOptions.SectionName)["Issuer"]))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
