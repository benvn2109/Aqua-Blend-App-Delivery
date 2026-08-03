using Microsoft.EntityFrameworkCore;
using AquaBlend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<AquaBlend.Services.ScenarioService>();
builder.Services.AddScoped<AquaBlend.Services.WaterSourceService>();
builder.Services.AddDbContext<AquaBlendDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AquaBlendDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// AquaBlend health-check endpoint
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
