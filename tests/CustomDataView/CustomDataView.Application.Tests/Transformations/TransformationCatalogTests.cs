using System.Collections;
using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Transformations.Dtos;
using CustomDataView.Application.Transformations.Queries;
using CustomDataView.Domain.Services.Transformations;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Application.Tests.Transformations;

public class TransformationCatalogTests
{
    [Test(Description = "scenario_TEMP-001-006 - #1")]
    public void Given_ConfiguredModule_When_UserBrowsesCatalog_Then_EightTransformationsAreSortedByKey()
    {
        var handler = CreateHandler();

        var transformations = handler.Handle(new GetTransformations());

        Assert.That(
            transformations.Select(transformation => transformation.Key),
            Is.EqualTo(new[] { "date_format", "lowercase", "mapper", "prefix", "round", "suffix", "truncate", "uppercase" }));
        Assert.That(
            transformations.Select(transformation => transformation.Name),
            Is.EqualTo(new[]
            {
                "Formatowanie daty", "Małe litery", "Słownik", "Prefiks", "Zaokrąglenie", "Sufiks", "Przycięcie tekstu", "Wielkie litery"
            }));
        Assert.That(transformations.Select(transformation => transformation.Description), Has.All.Not.Empty);
        Assert.That(
            typeof(TransformationParameterDto).GetProperties().Select(property => property.Name),
            Is.EquivalentTo(new[] { "Name", "Type" }));
    }

    [Test(Description = "scenario_TEMP-001-006 - #2")]
    public void Given_UserBrowsesCatalog_When_ChecksParameters_Then_EachTransformationDescribesItsParameter()
    {
        var handler = CreateHandler();

        var parameters = handler.Handle(new GetTransformations()).ToDictionary(transformation => transformation.Key, transformation => transformation.Parameter);

        Assert.That(parameters["round"], Is.EqualTo(new TransformationParameterDto("decimals", "integer")));
        Assert.That(parameters["date_format"], Is.EqualTo(new TransformationParameterDto("pattern", "string")));
        Assert.That(parameters["prefix"], Is.EqualTo(new TransformationParameterDto("text", "string")));
        Assert.That(parameters["suffix"], Is.EqualTo(new TransformationParameterDto("text", "string")));
        Assert.That(parameters["truncate"], Is.EqualTo(new TransformationParameterDto("max_length", "integer")));
        Assert.That(parameters["mapper"], Is.EqualTo(new TransformationParameterDto("dictionary", "string")));
        Assert.That(parameters["uppercase"], Is.Null);
        Assert.That(parameters["lowercase"], Is.Null);
    }

    [Test(Description = "scenario_TEMP-001-006 - #3")]
    public void Given_UserBrowsesCatalog_When_UserTriesToChangeTransformations_Then_CatalogIsReadOnly()
    {
        var handler = CreateHandler();
        var transformations = (IList)handler.Handle(new GetTransformations());

        Assert.That(
            typeof(GetTransformations).Assembly.GetTypes()
                .Where(type => type.Namespace?.StartsWith("CustomDataView.Application.Transformations", StringComparison.Ordinal) == true)
                .Where(IsCommand),
            Is.Empty);
        Assert.Throws<NotSupportedException>(() => transformations.Clear());
        Assert.That(handler.Handle(new GetTransformations()), Has.Count.EqualTo(8));
    }

    private static GetTransformationsHandler CreateHandler()
    {
        var provider = new ServiceCollection().AddCustomDataViewTransformations().BuildServiceProvider();
        return new GetTransformationsHandler(provider.GetServices<ITransformation>());
    }

    private static bool IsCommand(Type type)
        => type.GetInterfaces().Any(contract => contract == typeof(ICommand)
            || (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>)));
}
