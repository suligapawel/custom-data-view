using CustomDataView.Core.Transformations.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Core.Tests.Transformations;

public class TransformationCatalogServiceTests
{
    [Test(Description = "scenario_TEMP-001-006 - #1")]
    public void Given_ConfiguredModule_When_UserBrowsesCatalog_Then_TransformationsAreReturnedAsCatalogEntries()
    {
        var provider = new ServiceCollection().AddCustomDataViewTransformations().BuildServiceProvider();
        var service = provider.GetRequiredService<ITransformationCatalogService>();

        var round = service.GetAll().Single(transformation => transformation.Key == "round");

        Assert.That(round.Name, Is.EqualTo("Zaokrąglenie"));
        Assert.That(round.Description, Is.EqualTo("Zaokrągla liczbę do podanej liczby miejsc po przecinku (0-10)."));
        Assert.That(round.Parameters, Is.EqualTo(new[] { new TransformationParameterDto("decimals", "integer") }));
    }
}
