namespace CustomDataView.WebApi.Tests;

[SetUpFixture]
public sealed class WebApiTestHost
{
    public static CustomDataViewApplicationFactory Factory { get; private set; }

    [OneTimeSetUp]
    public void StartApplication()
    {
        Factory = new CustomDataViewApplicationFactory();
    }

    [OneTimeTearDown]
    public void StopApplication()
    {
        Factory?.Dispose();
    }
}
