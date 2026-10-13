using CustomDataView.Core.Transformations.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Core.Tests.Transformations;

public class TransformationCatalogTests
{
    [Test(Description = "scenario_TEMP-001-006 - #1")]
    public void Given_ConfiguredModule_When_UserBrowsesCatalog_Then_SevenTransformationsAreSortedByKey()
    {
        var catalog = CreateCatalog();

        var transformations = catalog.GetAll();

        Assert.That(
            transformations.Select(transformation => transformation.Key),
            Is.EqualTo(new[] { "date_format", "lowercase", "prefix", "round", "suffix", "truncate", "uppercase" }));
        Assert.That(
            transformations.Select(transformation => transformation.Name),
            Is.EqualTo(new[]
            {
                "Formatowanie daty", "Małe litery", "Prefiks", "Zaokrąglenie", "Sufiks", "Przycięcie tekstu", "Wielkie litery"
            }));
    }

    private static ITransformationCatalog CreateCatalog()
    {
        var provider = new ServiceCollection().AddCustomDataViewTransformations().BuildServiceProvider();
        return provider.GetRequiredService<ITransformationCatalog>();
    }
}
