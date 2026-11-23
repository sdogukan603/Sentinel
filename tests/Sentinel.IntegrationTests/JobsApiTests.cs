
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sentinel.IntegrationTests;

public sealed class JobsApiTests :
    IClassFixture<SentinelApiFactory>
{
    private readonly HttpClient _client;

    public JobsApiTests(SentinelApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_Returns200OK()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SENTINEL", content);
    }

    [Fact]
    public async Task CreateJob_Returns201Created()
    {
        var response = await CreateJobAsync("Create Test");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateJob_WithEmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/jobs",
            new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatedJob_CanBeRetrieved()
    {
        var createResponse = await CreateJobAsync("Retrieve Test");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var getResponse = await _client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "Retrieve Test",
            json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task StartJobTwice_Returns409Conflict()
    {
        var createResponse = await CreateJobAsync("Start Test");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var first = await _client.PostAsync(
            $"{location}/start", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsync(
            $"{location}/start", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RunningJob_CanBeCompleted()
    {
        var createResponse = await CreateJobAsync("Complete Test");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var start = await _client.PostAsync(
            $"{location}/start", null);

        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        var complete = await _client.PostAsync(
            $"{location}/complete", null);

        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        using var json = JsonDocument.Parse(
            await complete.Content.ReadAsStringAsync());

        Assert.Equal(
            2,
            json.RootElement.GetProperty("status").GetInt32());
    }

    private Task<HttpResponseMessage> CreateJobAsync(string name)
    {
        return _client.PostAsJsonAsync(
            "/api/jobs",
            new { Name = name });
    }
}
