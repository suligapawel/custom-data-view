using CustomDataView.Application.Tiles.Contracts;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Application.Views.Validation;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Transformations;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomDataViewApplication(this IServiceCollection services)
    {
        services.AddCustomDataViewTransformations();
        services.AddSingleton<TileReferencesValidator>();
        services.AddSingleton<TilePreviewer>();
        services.AddSingleton<TileContracts>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ViewReferencesValidator>();
        return services;
    }

    public static IServiceCollection AddCustomDataViewTransformations(this IServiceCollection services)
    {
        services.AddSingleton<ITransformation, DateFormatTransformation>();
        services.AddSingleton<ITransformation, LowercaseTransformation>();
        services.AddSingleton<ITransformation, PrefixTransformation>();
        services.AddSingleton<ITransformation, RoundTransformation>();
        services.AddSingleton<ITransformation, SuffixTransformation>();
        services.AddSingleton<ITransformation, TruncateTransformation>();
        services.AddSingleton<ITransformation, UppercaseTransformation>();
        services.AddSingleton<ITransformationService, TransformationService>();
        return services;
    }
}
