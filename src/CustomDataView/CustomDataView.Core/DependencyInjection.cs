using CustomDataView.Core.Transformations;
using CustomDataView.Core.Transformations.Catalog;
using CustomDataView.Core.Transformations.DateFormat;
using CustomDataView.Core.Transformations.Lowercase;
using CustomDataView.Core.Transformations.Prefix;
using CustomDataView.Core.Transformations.Round;
using CustomDataView.Core.Transformations.Suffix;
using CustomDataView.Core.Transformations.Truncate;
using CustomDataView.Core.Transformations.Uppercase;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomDataViewTransformations(this IServiceCollection services)
    {
        services.AddSingleton<ITransformationDefinition, DateFormatTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, LowercaseTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, PrefixTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, RoundTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, SuffixTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, TruncateTransformationDefinition>();
        services.AddSingleton<ITransformationDefinition, UppercaseTransformationDefinition>();
        services.AddSingleton<ITransformationCatalog, TransformationCatalog>();
        return services;
    }
}
