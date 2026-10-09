using System.Net;

namespace SchedulingApp.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_endpoint_reports_database_connectivity()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Openapi_document_is_published()
    {
        var json = await factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/appointments", json);
    }
}
