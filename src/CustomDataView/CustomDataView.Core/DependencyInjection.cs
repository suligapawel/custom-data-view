using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Catalog;
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
        services.AddSingleton<ITransformationCatalog, TransformationCatalog>();
        services.AddSingleton<ITransformationCatalogService, TransformationCatalogService>();
        return services;
    }
}
