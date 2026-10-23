using CustomDataView.Application;
using CustomDataView.Bootstrapper.Development;
using CustomDataView.Infrastructure;
using CustomDataView.WebApi.Endpoints;
using CustomDataView.WebApi.Errors;
using Scalar.AspNetCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(options => options.Discovery.IncludeAssembly(typeof(CustomDataView.Application.DependencyInjection).Assembly));

builder.Services.AddCustomDataViewApplication();
builder.Services.AddCustomDataViewInfrastructure();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<CustomDataViewExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapFakeWeatherEndpoints();
}

app.MapTransformationEndpoints();
app.MapConnectionEndpoints();
app.MapTileEndpoints();
app.MapViewEndpoints();

app.Run();

public partial class Program;
