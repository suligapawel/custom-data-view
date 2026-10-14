namespace CustomDataView.Api.Tests.Views;

public class ViewDetailsTests : ViewApiTests
{
    [Test(Description = "scenario_TEMP-024-028 - #19")]
    public async Task Given_ViewWithTwoTiles_When_UserViewsDetails_Then_TilesAreInPositionOrderWithIdNamePositionAndCssClass()
    {
        var id = await CreateView("Pulpit klienta", ViewTile(RateTileId, 1), ViewTile(CustomerTileId, 0, "col-6"));

        var view = await GetView(id);

        Assert.That(view["id"]?.GetValue<Guid>(), Is.EqualTo(id));
        Assert.That(view["name"]?.GetValue<string>(), Is.EqualTo("Pulpit klienta"));
        Assert.That(Tiles(view), Is.EqualTo(new (Guid?, string, int?, string)[]
        {
            (CustomerTileId, "Klient", 0, "col-6"),
            (RateTileId, "Kurs walut", 1, null)
        }));
    }
}
