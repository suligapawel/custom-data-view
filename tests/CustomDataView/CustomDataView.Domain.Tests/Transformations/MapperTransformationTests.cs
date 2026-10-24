using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Transformations;

public class MapperTransformationTests
{
    private const string Dir = "scenario_TEMP-045";

    private static readonly Guid DictionaryTileId = Guid.Parse("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70");

    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70:Tłumaczenie", "Tłumaczenie")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70:Czas: lokalny", "Czas: lokalny")]
    [Test(Description = $"{Dir} - #14")]
    public void Given_ParameterWithTileIdAndLabel_When_ItIsParsed_Then_ReferenceHasTileAndValueLabel(string parameter, string label)
    {
        var parsed = DictionaryReference.TryParse(parameter, out var reference);

        Assert.That(parsed, Is.True);
        Assert.That(reference, Is.EqualTo(new DictionaryReference(DictionaryTileId, label)));
    }

    [TestCase(null)]
    [TestCase("Statusy zamówień")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70Tłumaczenie")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2bXX:Tłumaczenie")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70:")]
    [TestCase("4f0c2a51-8a3e-4c55-9c41-3f8a3a1d2b70:   ")]
    [Test(Description = $"{Dir} - #14")]
    public void Given_ParameterInWrongFormat_When_ItIsParsed_Then_ItIsRejected(string parameter)
    {
        var parsed = DictionaryReference.TryParse(parameter, out _);

        Assert.That(parsed, Is.False);
    }

    [Test(Description = $"{Dir} - #2")]
    public void Given_ReferencesDifferingInLabelCaseAndSpaces_When_TheyAreCompared_Then_TheyAreEqual()
    {
        var first = new DictionaryReference(DictionaryTileId, "Tłumaczenie");
        var second = new DictionaryReference(DictionaryTileId, " tłumaczenie ");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test(Description = $"{Dir} - #2")]
    public void Given_LoadedDictionary_When_ValueIsMapped_Then_DictionaryValueIsReturned()
    {
        var mapper = Mapper(("NEW", JsonValue.Create("Nowe")));

        var result = mapper.Apply("NEW", Parameter());

        Assert.That(result, Is.EqualTo("Nowe"));
    }

    [Test(Description = $"{Dir} - #3")]
    public void Given_DictionaryWithNumberValue_When_ValueIsMapped_Then_NumberIsReturned()
    {
        var mapper = Mapper((6m, JsonValue.Create(8m)));

        var result = mapper.Apply(6m, Parameter());

        Assert.That(result, Is.EqualTo(8m));
    }

    [TestCase(6, "6")]
    [TestCase(6.5, "6.5")]
    [Test(Description = $"{Dir} - #4")]
    public void Given_DictionaryKeyWrittenAsText_When_NumberIsMapped_Then_NumberFindsKey(double number, string key)
    {
        var mapper = Mapper((key, JsonValue.Create("wysoki")));

        var result = mapper.Apply((decimal)number, Parameter());

        Assert.That(result, Is.EqualTo("wysoki"));
    }

    [Test(Description = $"{Dir} - #7")]
    public void Given_ValueMissingFromDictionary_When_ItIsMapped_Then_MissingKeyIsReported()
    {
        var mapper = Mapper(("NEW", JsonValue.Create("Nowe")));

        var exception = Assert.Throws<TransformationFailedException>(() => mapper.Apply("LOST", Parameter()));

        Assert.That((exception.Key, exception.Reason), Is.EqualTo(("mapper", TransformationFailureReasons.MissingDictionaryKey)));
    }

    [Test(Description = $"{Dir} - #9")]
    public void Given_DictionaryNotLoaded_When_ValueIsMapped_Then_UnavailableDictionaryIsReported()
    {
        var mapper = new MapperTransformation(new LoadedDictionaries());

        var exception = Assert.Throws<TransformationFailedException>(() => mapper.Apply("NEW", Parameter()));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.DictionaryUnavailable));
    }

    [Test(Description = $"{Dir} - #14")]
    public void Given_ParameterInWrongFormat_When_ValueIsMapped_Then_InvalidParameterIsReported()
    {
        var mapper = Mapper(("NEW", JsonValue.Create("Nowe")));

        var exception = Assert.Throws<TransformationFailedException>(() => mapper.Apply("NEW", "Statusy zamówień"));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidParameter));
    }

    [Test(Description = $"{Dir} - #7")]
    public void Given_ObjectValue_When_ItIsMapped_Then_InvalidValueIsReported()
    {
        var mapper = Mapper(("NEW", JsonValue.Create("Nowe")));

        var exception = Assert.Throws<TransformationFailedException>(() => mapper.Apply(new JsonObject { ["key"] = "NEW" }, Parameter()));

        Assert.That(exception.Reason, Is.EqualTo(TransformationFailureReasons.InvalidValue));
    }

    [Test(Description = $"{Dir} - #10")]
    public void Given_EntriesWithRepeatedKey_When_DictionaryIsBuilt_Then_ItIsRejected()
    {
        var built = MapperDictionary.TryBuild([("NEW", JsonValue.Create("Nowe")), ("NEW", JsonValue.Create("Inne"))], out _);

        Assert.That(built, Is.False);
    }

    [Test(Description = $"{Dir} - #10")]
    public void Given_EntryWithObjectKey_When_DictionaryIsBuilt_Then_ItIsRejected()
    {
        var built = MapperDictionary.TryBuild([(new JsonObject(), JsonValue.Create("Nowe"))], out _);

        Assert.That(built, Is.False);
    }

    [Test(Description = $"{Dir} - #4")]
    public void Given_NumberAndTextKeysOfSameValue_When_DictionaryIsBuilt_Then_TheyAreRepeatedKey()
    {
        var built = MapperDictionary.TryBuild([(6m, JsonValue.Create("niski")), ("6", JsonValue.Create("wysoki"))], out _);

        Assert.That(built, Is.False);
    }

    [Test(Description = $"{Dir} - #3")]
    public void Given_DictionaryWithObjectValue_When_ValueIsReadTwice_Then_EachReadIsSeparateCopy()
    {
        MapperDictionary.TryBuild([("NEW", new JsonObject { ["name"] = "Nowe" })], out var dictionary);

        dictionary.TryGet("NEW", out var first);
        ((JsonObject)first)["name"] = "Zmienione";
        dictionary.TryGet("NEW", out var second);

        Assert.That(((JsonObject)second)["name"]!.GetValue<string>(), Is.EqualTo("Nowe"));
    }

    private static string Parameter() => $"{DictionaryTileId}:Tłumaczenie";

    private static MapperTransformation Mapper(params (object Key, JsonNode Value)[] entries)
    {
        MapperDictionary.TryBuild(entries, out var dictionary);
        var dictionaries = new LoadedDictionaries();
        dictionaries.Add(new DictionaryReference(DictionaryTileId, "Tłumaczenie"), dictionary);
        return new MapperTransformation(dictionaries);
    }

    private sealed class LoadedDictionaries : IMapperDictionaries
    {
        private readonly Dictionary<DictionaryReference, MapperDictionary> dictionaries = [];

        public void Add(DictionaryReference reference, MapperDictionary dictionary) => dictionaries[reference] = dictionary;

        public bool TryGet(DictionaryReference reference, out MapperDictionary dictionary)
            => dictionaries.TryGetValue(reference, out dictionary);
    }
}
