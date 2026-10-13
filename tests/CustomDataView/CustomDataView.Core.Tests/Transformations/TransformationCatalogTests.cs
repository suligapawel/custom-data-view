using System.Collections;
using CustomDataView.Core.Transformations;
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
        Assert.That(transformations.Select(transformation => transformation.Description), Has.All.Not.Empty);
        Assert.That(
            typeof(TransformationParameterDescriptor).GetProperties().Select(property => property.Name),
            Is.EquivalentTo(new[] { "Name", "Type" }));
    }

    [Test(Description = "scenario_TEMP-001-006 - #2")]
    public void Given_UserBrowsesCatalog_When_ChecksParameters_Then_EachTransformationDescribesItsParameters()
    {
        var catalog = CreateCatalog();

        var parameters = catalog.GetAll().ToDictionary(transformation => transformation.Key, transformation => transformation.Parameters);

        Assert.That(parameters["round"], Is.EqualTo(new[] { new TransformationParameterDescriptor("decimals", "integer") }));
        Assert.That(parameters["date_format"], Is.EqualTo(new[] { new TransformationParameterDescriptor("pattern", "string") }));
        Assert.That(parameters["prefix"], Is.EqualTo(new[] { new TransformationParameterDescriptor("text", "string") }));
        Assert.That(parameters["suffix"], Is.EqualTo(new[] { new TransformationParameterDescriptor("text", "string") }));
        Assert.That(parameters["truncate"], Is.EqualTo(new[] { new TransformationParameterDescriptor("max_length", "integer") }));
        Assert.That(parameters["uppercase"], Is.Empty);
        Assert.That(parameters["lowercase"], Is.Empty);
    }

    [Test(Description = "scenario_TEMP-001-006 - #3")]
    public void Given_UserBrowsesCatalog_When_UserTriesToChangeTransformations_Then_CatalogIsReadOnly()
    {
        var catalog = CreateCatalog();
        var transformations = (IList)catalog.GetAll();

        Assert.That(
            typeof(ITransformationCatalog).GetMethods().Select(method => method.Name),
            Is.EquivalentTo(new[] { nameof(ITransformationCatalog.GetAll), nameof(ITransformationCatalog.Find) }));
        Assert.Throws<NotSupportedException>(() => transformations.Clear());
        Assert.That(catalog.GetAll(), Has.Count.EqualTo(7));
    }

    private static ITransformationCatalog CreateCatalog()
    {
        var provider = new ServiceCollection().AddCustomDataViewTransformations().BuildServiceProvider();
        return provider.GetRequiredService<ITransformationCatalog>();
    }
}
