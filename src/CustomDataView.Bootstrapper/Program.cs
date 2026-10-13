using CustomDataView.Api.Endpoints;
using CustomDataView.Api.Errors;
using CustomDataView.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomDataViewTransformations();
builder.Services.AddCustomDataViewConnections();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomDataViewExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapTransformationEndpoints();
app.MapConnectionEndpoints();

app.Run();

public partial class Program;
