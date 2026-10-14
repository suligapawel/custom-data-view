using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Management;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Connections.Testing;
using CustomDataView.Core.Tiles.Management;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Tiles.Preview;
using CustomDataView.Core.Tiles.Validation;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Catalog;
using CustomDataView.Core.Transformations.Execution;
using CustomDataView.Core.Transformations.Implementations;
using CustomDataView.Core.Views.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CustomDataView.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomDataViewTransformations(this IServiceCollection services)
    {
        services.AddSingleton<ITransformation, DateFormatTransformation>();
        services.AddSingleton<ITransformation, LowercaseTransformation>();
        services.AddSingleton<ITransformation, PrefixTransformation>();
        services.AddSingleton<ITransformation, RoundTransformation>();
        services.AddSingleton<ITransformation, SuffixTransformation>();
        services.AddSingleton<ITransformation, TruncateTransformation>();
        services.AddSingleton<ITransformation, UppercaseTransformation>();
        services.AddSingleton<ITransformationCatalogService, TransformationCatalogService>();
        services.AddSingleton<ITransformationService, TransformationService>();
        return services;
    }

    public const string ConnectionStringName = "CustomDataView";

    public static IServiceCollection AddCustomDataViewConnections(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string \"{ConnectionStringName}\" is not configured.")));
        services.AddSingleton<IConnectionRepository, PostgresConnectionRepository>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IConnectionTestService, ConnectionTestService>();
        services.AddSingleton<IConnectionFactory, ConnectionFactory>();
        services.AddSingleton<IExternalApiClient, ExternalApiClient>();
        services.AddOptions<ExternalApiOptions>().BindConfiguration(ExternalApiOptions.SectionName);
        services.AddHttpClient(ExternalApiClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        return services;
    }

    public static IServiceCollection AddCustomDataViewTiles(this IServiceCollection services)
    {
        services.AddSingleton<ITileRepository, PostgresTileRepository>();
        services.AddSingleton<TileReferencesValidator>();
        services.AddSingleton<ITileService, TileService>();
        services.AddSingleton<ITilePreviewService, TilePreviewService>();
        return services;
    }

    public static IServiceCollection AddCustomDataViewViews(this IServiceCollection services)
    {
        services.AddSingleton<IViewService, ViewService>();
        return services;
    }
}
