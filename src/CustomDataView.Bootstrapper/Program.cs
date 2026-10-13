using CustomDataView.Api.Endpoints;
using CustomDataView.Api.Errors;
using CustomDataView.Bootstrapper.Development;
using CustomDataView.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomDataViewTransformations();
builder.Services.AddCustomDataViewConnections();
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

app.Run();

public partial class Program;
