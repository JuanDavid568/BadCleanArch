using System.Text.Json;
using Application.UseCases;
using Domain.Entities;
using Domain.Services;
using Infrastructure.Data;
using Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var allowedOrigins= builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(o => o.AddPolicy("DefaultCors", p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("Orders") ?? "Data Source=App_Data/Orders.db";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title= "Orders API", Version = "v1" });
    
});

builder.Services.AddSingleton<IOrderRepository>(_ => new SqliteOrderRepository(connectionString));
builder.Services.AddSingleton<IAppLogger, Logger>();
builder.Services.AddScoped<CreateOrderUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (ex is DomainException or JsonException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<IAppLogger>();
        logger.Log($"Error no controlado: {ex.Message}");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Error interno del servidor." });
    }
});

app.MapControllers();

await app.RunAsync();