using CustomDataView.Api.Endpoints;
using CustomDataView.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomDataViewTransformations();
builder.Services.AddCustomDataViewConnections();

var app = builder.Build();

app.MapTransformationEndpoints();
app.MapConnectionEndpoints();

app.Run();

public partial class Program;
