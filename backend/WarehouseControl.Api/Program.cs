using System.Text.Json.Serialization;
using WarehouseControl.Api.State;

const string FrontendDevCorsPolicy = "FrontendDev";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WarehouseState>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors(FrontendDevCorsPolicy);

app.Services.GetRequiredService<WarehouseState>().Seed();

app.MapGet("/", () => "Hello World!");

app.Run();

// Exposes the generated Program class to WebApplicationFactory<Program> in the test project.
public partial class Program;
