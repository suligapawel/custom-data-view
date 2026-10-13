using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Management;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Connections.Testing;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Catalog;
using CustomDataView.Core.Transformations.Execution;
using CustomDataView.Core.Transformations.Implementations;
using Microsoft.Extensions.DependencyInjection;

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

    public static IServiceCollection AddCustomDataViewConnections(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionRepository, InMemoryConnectionRepository>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IConnectionTestService, ConnectionTestService>();
        services.AddSingleton<IConnectionFactory, ConnectionFactory>();
        services.AddSingleton<IExternalApiClient, ExternalApiClient>();
        services.AddHttpClient(ExternalApiClient.HttpClientName);
        return services;
    }
}
