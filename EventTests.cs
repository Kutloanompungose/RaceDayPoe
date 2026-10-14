using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EventTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await LoginAs("Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", EventBody("Soweto 10km"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Soweto 10km", json.GetProperty("name").GetString());
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoginAs("Participant");
        var res = await participant.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await LoginAs("Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad", eventDate = DateTime.UtcNow.AddDays(10), province = "Gauteng", venue = "X", eventType = "Swim"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organiser = await LoginAs("Organiser");
        var id = await CreateEvent(organiser);
        var participant = await LoginAs("Participant");

        foreach (var client in new[] { organiser, participant })
        {
            var list = await client.GetAsync("/api/events");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);

            var single = await client.GetAsync($"/api/events/{id}");
            Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        }
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var res = await Anonymous().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await LoginAs("Organiser");
        var id = await CreateEvent(organiser);

        var res = await organiser.PutAsJsonAsync($"/api/events/{id}", EventBody("Renamed Event"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed Event", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoginAs("Organiser");
        var other = await LoginAs("Organiser");
        var id = await CreateEvent(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/events/{id}", EventBody("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_EvenWithEnrolments()
    {
        var organiser = await LoginAs("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var participant = await LoginAs("Participant");
        await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvents_Returns403()
    {
        var organiser = await LoginAs("Organiser");
        var id = await CreateEvent(organiser);
        var participant = await LoginAs("Participant");

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.PutAsJsonAsync($"/api/events/{id}", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    // ---------- Categories ----------
    [Fact]
    public async Task Organiser_CanManageCategories_ParticipantCannot()
    {
        var organiser = await LoginAs("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}",
            new { name = "21km", distanceKm = 21.1, maxParticipants = 50, entryFee = 300 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var participant = await LoginAs("Participant");
        var forbiddenCreate = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "5km", distanceKm = 5, maxParticipants = 10, entryFee = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);

        // Both roles can view categories
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{eventId}/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync($"/api/events/{eventId}/categories")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent()
    {
        var owner = await LoginAs("Organiser");
        var other = await LoginAs("Organiser");
        var eventId = await CreateEvent(owner);

        var res = await other.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "5km", distanceKm = 5, maxParticipants = 10, entryFee = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidDistance_Returns400()
    {
        var organiser = await LoginAs("Organiser");
        var eventId = await CreateEvent(organiser);
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Bad", distanceKm = 0, maxParticipants = 10, entryFee = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
