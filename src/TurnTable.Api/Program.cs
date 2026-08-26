using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using TurnTable.Api.Data;
using TurnTable.Api.Middleware;
using TurnTable.Api.Services;
using TurnTable.Api.Hubs;

namespace TurnTable.Api;

/// <summary>
/// Main application entry point for the TurnTable Web API.
/// Configures dependency injection, security policies, middleware pipeline, and endpoint routing.
/// </summary>
public class Program
{
    /// <summary>
    /// Program execution entry point.
    /// </summary>
    /// <param name="args">Command line launch arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Register Controllers for handling incoming HTTP requests
        builder.Services.AddControllers();

        // 2. Register Entity Framework Core & PostgreSQL Database
        builder.Services.AddDbContext<TurnTableDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Register the database-backed store (scoped per client HTTP request)
        builder.Services.AddScoped<ITurnTableService, DbTurnTableStore>();

        // Register SignalR Services
        builder.Services.AddSignalR();

        // 3. Configure CORS to allow frontend SPAs (Vercel and Localhost)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontendApp", policy =>
            {
                policy.SetIsOriginAllowed(origin => 
                    origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) || 
                    origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase) || 
                    origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for SignalR websockets
            });
        });

        // 4. Register Swagger/OpenAPI documentation services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "TurnTable Host Stand Seating System API",
                Version = "v1",
                Description = "REST API for managing tables, waitlists, server shift assignments, and round-robin section rotations."
            });
        });

        var app = builder.Build();

        // 5. Global Exception Handling Middleware (First in pipeline to catch all downstream errors)
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // 6. Enable Swagger UI in Development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TurnTable API v1");
                c.RoutePrefix = string.Empty;  // serves Swagger UI at application root (http://localhost:PORT/)
            });
        }

        // 7. Apply CORS policy
        app.UseCors("AllowFrontendApp");

        // 8. HTTPS Redirection & Authorization
        app.UseHttpsRedirection();
        app.UseAuthorization();

        // 9. Map Controller Endpoints
        app.MapControllers();
        app.MapHub<TurnTableHub>("/hub/turntable");

        // 10. Start Web Server
        app.Run();
    }
}