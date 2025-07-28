using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Webjet.Movie.API.Common.Behaviors;
using Webjet.Movie.API.Common.Exceptions;
using Webjet.Movie.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var assembly = typeof(Program).Assembly;

// Add MediatR
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(assembly);

// Add Carter for modular endpoints
builder.Services.AddCarter();

// Add exception handler
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

// Add memory cache
builder.Services.AddMemoryCache();

// Configure settings
builder.Services.Configure<MovieProvidersConfig>(builder.Configuration.GetSection("MovieProviders"));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));

// Add HTTP clients for providers
builder.Services.AddHttpClient<CinemaWorldClient>();
builder.Services.AddHttpClient<FilmWorldClient>();

// Register providers types and interfaces
builder.Services.AddScoped<CinemaWorldClient>();
builder.Services.AddScoped<FilmWorldClient>();
builder.Services.AddScoped<IProviderClient>(sp => sp.GetRequiredService<CinemaWorldClient>());
builder.Services.AddScoped<IProviderClient>(sp => sp.GetRequiredService<FilmWorldClient>());

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(options => { });

// Use CORS
app.UseCors();

app.MapCarter();

app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

app.Run();
