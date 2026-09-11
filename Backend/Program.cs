using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using AquaBlend.Authorization;
using AquaBlend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using AquaBlend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT Bearer token."
            };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var requiresAuthorization = context.Description.ActionDescriptor
            .EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (requiresAuthorization)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddControllers();
builder.Services.AddScoped<OptimisationResultService>();
builder.Services.AddScoped<AquaBlend.Services.ScenarioService>();
builder.Services.AddScoped<AquaBlend.Services.WaterSourceService>();

const string AquaBlendFrontendPolicy = "AquaBlendFrontend";
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AquaBlendFrontendPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var useInMemoryDatabase = builder.Environment.IsEnvironment("Testing");
var inMemoryDatabaseName =
    builder.Configuration.GetValue<string>("InMemoryDatabaseName")
    ?? "AquaBlendTestDb";

builder.Services.AddDbContext<AquaBlendDbContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase(inMemoryDatabaseName);
    }
    else
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AppPolicies.CanView,
        policy => policy.RequireRole(
            AppRoles.Admin,
            AppRoles.Analyst,
            AppRoles.Viewer));

    options.AddPolicy(
        AppPolicies.CanAnalyse,
        policy => policy.RequireRole(
            AppRoles.Admin,
            AppRoles.Analyst));

    options.AddPolicy(
        AppPolicies.CanAdminister,
        policy => policy.RequireRole(AppRoles.Admin));
});

var app = builder.Build();

// Apply migrations and seed data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AquaBlendDbContext>();

    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    SeedData.Initialize(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(AquaBlendFrontendPolicy);
app.UseAuthentication();
app.UseAuthorization();

// AquaBlend health-check endpoint.
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "AquaBlend.Api",
        timestamp = DateTime.UtcNow
    });
})
.WithName("GetHealth");

app.MapControllers();

app.Run();

public partial class Program { }
