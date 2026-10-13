using CustomDataView.Api.Endpoints;
using CustomDataView.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomDataViewTransformations();

var app = builder.Build();

app.MapTransformationEndpoints();

app.Run();

public partial class Program;
