using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Api.Tests.Tiles;

public class TilePartialPreviewTests : TileApiTests
{
    [Test(Description = "scenario_TEMP-015-023 - #44")]
    public async Task Given_RateSourceNotResponding_When_UserPreviewsCustomerAndRateTile_Then_OnlyRateFieldHasSourceError()
    {
        var id = await CreateTile(new
        {
            name = "Klient i kurs",
            fields = new[] { Field(CustomerConnectionId, "$.name", "Nazwa"), Field(RateConnectionId, "$.rate", "Kurs EUR") }
        });
        RateSourceFails(_ => throw new HttpRequestException("Connection refused"));

        var preview = await Preview(id, CustomerAndRateValues());

        Assert.That(
            JsonNode.DeepEquals(
                preview["fields"],
                JsonNode.Parse("""
                               [
                                 {"label": "Nazwa", "value": "acme"},
                                 {"label": "Kurs EUR", "error": {"code": "source_unavailable"}}
                               ]
                               """)),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-015-023 - #45")]
    public async Task Given_RateSourceRespondingWith503_When_UserPreviewsRateTile_Then_FieldHasSourceErrorWithReasonAndStatus()
    {
        var id = await CreateTile(new { name = "Kurs", fields = new[] { Field(RateConnectionId, "$.rate", "Kurs EUR") } });
        RateSourceFails(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var preview = await Preview(id, RateValues());

        Assert.That(
            JsonNode.DeepEquals(
                preview["fields"]![0]!["error"],
                JsonNode.Parse("""{"code": "source_unavailable", "reason": "unexpected_status", "status": 503}""")),
            Is.True,
            preview.ToJsonString());
    }

    [Test(Description = "scenario_TEMP-015-023 - #46")]
    public async Task Given_FieldsForExistingCityAndMissingZip_When_UserPreviewsTile_Then_ZipFieldHasPathNotFound()
    {
        var id = await CreateTile(new
        {
            name = "Adres",
            fields = new[] { Field(CustomerConnectionId, "$.address.city", "Miasto"), Field(CustomerConnectionId, "$.address.zip", "Kod") }
        });

        var preview = await Preview(id, CustomerValues());

        AssertFields(preview, """
                              [
                                {"label": "Miasto", "value": "Kraków"},
                                {"label": "Kod", "error": {"code": "path_not_found"}}
                              ]
                              """);
    }

    private static void AssertFields(JsonObject preview, string expected)
        => Assert.That(JsonNode.DeepEquals(preview["fields"], JsonNode.Parse(expected)), Is.True, preview.ToJsonString());

    private object RateValues()
        => new { connections = new[] { new { connectionId = RateConnectionId, query = new { currency = "EUR" } } } };

    private void RateSourceFails(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var respondNormally = Factory.ExternalApi.Respond;
        Factory.ExternalApi.Respond = (request, cancellationToken) => request.RequestUri!.AbsoluteUri.StartsWith(RateUrl.Split('?')[0])
            ? Task.FromResult(respond(request))
            : respondNormally(request, cancellationToken);
    }

    private async Task<JsonObject> Preview(Guid id, object values)
    {
        var response = await Client.PostAsJsonAsync($"{TilesUrl}/{id}/preview", values);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
