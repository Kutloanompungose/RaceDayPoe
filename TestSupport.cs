using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Hosts the real API in memory, but swaps SQL Server for an EF Core in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var existing = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var d in existing) services.Remove(d);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

/// <summary>Helpers shared by all API tests. Each test registers its own users, so tests never collide.</summary>
public abstract class ApiTestBase(ApiFactory factory) : IClassFixture<ApiFactory>
{
    protected const string Password = "Passw0rd!";
    protected readonly ApiFactory Factory = factory;

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    protected async Task<HttpClient> LoginAs(string role, string? email = null)
    {
        var client = Factory.CreateClient();
        email ??= NewEmail();

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test " + role, email, password = Password, role });
        Assert.Equal(System.Net.HttpStatusCode.Created, reg.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }

    protected static async Task<int> IdOf(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    protected static object EventBody(string name = "Test Fun Run") => new
    {
        name, description = "A test event", eventDate = DateTime.UtcNow.AddMonths(3),
        province = "Gauteng", venue = "Johannesburg Stadium", eventType = "Run"
    };

    protected async Task<int> CreateEvent(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(System.Net.HttpStatusCode.Created, res.StatusCode);
        return await IdOf(res, "eventID");
    }

    protected async Task<int> CreateCategory(HttpClient organiser, int eventId, int max = 100)
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "10km", distanceKm = 10, maxParticipants = max, entryFee = 150 });
        Assert.Equal(System.Net.HttpStatusCode.Created, res.StatusCode);
        return await IdOf(res, "categoryID");
    }

    protected async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var res = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(System.Net.HttpStatusCode.Created, res.StatusCode);
        return await IdOf(res, "enrolmentID");
    }
}
