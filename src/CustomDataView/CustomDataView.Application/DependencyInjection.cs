using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.Services.Transformations;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomDataViewApplication(this IServiceCollection services)
    {
        services.AddCustomDataViewTransformations();
        services.AddCustomDataViewCombinations();
        services.AddSingleton<TileReferencesValidator>();
        services.AddSingleton<TilePreviewer>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static IServiceCollection AddCustomDataViewTransformations(this IServiceCollection services)
    {
        services.AddSingleton<ITransformation, DateFormatTransformation>();
        services.AddSingleton<ITransformation, LowercaseTransformation>();
        services.AddSingleton<ITransformation, MapperTransformation>();
        services.AddSingleton<ITransformation, PrefixTransformation>();
        services.AddSingleton<ITransformation, RoundTransformation>();
        services.AddSingleton<ITransformation, SuffixTransformation>();
        services.AddSingleton<ITransformation, TruncateTransformation>();
        services.AddSingleton<ITransformation, UppercaseTransformation>();
        services.AddSingleton<ITransformationService, TransformationService>();
        return services;
    }

    public static IServiceCollection AddCustomDataViewCombinations(this IServiceCollection services)
    {
        services.AddSingleton<ICombination, CoalesceCombination>();
        services.AddSingleton<ICombination, JoinCombination>();
        services.AddSingleton<ICombination, ObjectCombination>();
        services.AddSingleton<ICombination, TemplateCombination>();
        services.AddSingleton<CombinationService>();
        return services;
    }
}
