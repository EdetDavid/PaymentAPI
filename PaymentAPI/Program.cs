using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var databaseHost = builder.Configuration["PGHOST"];
var databaseName = builder.Configuration["PGDATABASE"];
var databaseUser = builder.Configuration["PGUSER"];
var databasePassword = builder.Configuration["PGPASSWORD"];

if (string.IsNullOrWhiteSpace(databaseHost) ||
    string.IsNullOrWhiteSpace(databaseName) ||
    string.IsNullOrWhiteSpace(databaseUser) ||
    string.IsNullOrWhiteSpace(databasePassword))
{
    throw new InvalidOperationException("Neon database settings PGHOST, PGDATABASE, PGUSER, and PGPASSWORD are required.");
}

var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = databaseHost,
    Database = databaseName,
    Username = databaseUser,
    Password = databasePassword,
    SslMode = SslMode.Require
}.ConnectionString;

builder.Services.AddDbContext<PaymentDetailContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(options =>
options.WithOrigins("http://localhost:4200")
.AllowAnyMethod()
.AllowAnyHeader()
);

app.UseAuthorization();

// The existing payment controller persists card numbers and security codes.
// Keep it unavailable outside local development until a payment provider replaces it.
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api/PaymentDetail", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/db", async (PaymentDetailContext database) =>
    await database.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapControllers();

app.Run();
