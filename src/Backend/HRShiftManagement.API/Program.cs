using HRShiftManagement.API.Extensions;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Register Infrastructure services (EF Core, Repositories, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register JWT Bearer Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed initial administrator user if configured
try
{
    using var scope = app.Services.CreateScope();
    var adminBootstrap = scope.ServiceProvider.GetRequiredService<IAdminBootstrapService>();
    await adminBootstrap.SeedAdminAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Admin bootstrap encountered an exception during startup.");
}

app.Run();

public partial class Program { }
