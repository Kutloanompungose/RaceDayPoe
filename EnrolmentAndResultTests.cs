using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EnrolmentTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Participant_CanEnrol_AndGetsRaceNumber()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoginAs("Participant");

        var res = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(categoryId, json.GetProperty("categoryID").GetInt32());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("raceNumber").GetString()));
    }

    [Fact]
    public async Task Enrolment_AppearsInParticipantHistory()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoginAs("Participant");
        var enrolmentId = await Enrol(participant, categoryId);

        var history = await (await participant.GetAsync("/api/enrolments/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(history.EnumerateArray(), e => e.GetProperty("enrolmentID").GetInt32() == enrolmentId);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoginAs("Participant");
        await Enrol(participant, categoryId);

        var second = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser), max: 1);
        await Enrol(await LoginAs("Participant"), categoryId);

        var late = await (await LoginAs("Participant")).PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await LoginAs("Participant");
        var res = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = 999999 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));

        var res = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ButNotSomeoneElses()
    {
        var organiser = await LoginAs("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var owner = await LoginAs("Participant");
        var other = await LoginAs("Participant");
        var enrolmentId = await Enrol(owner, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEventEnrolments_ParticipantCannot()
    {
        var organiser = await LoginAs("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var participant = await LoginAs("Participant");
        await Enrol(participant, categoryId);

        var ok = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var list = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, list.GetArrayLength());

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await LoginAs("Organiser")).GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }
}

public class ResultTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<(HttpClient organiser, HttpClient participant, int eventId, int enrolmentId)> Arrange()
    {
        var organiser = await LoginAs("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var participant = await LoginAs("Participant");
        var enrolmentId = await Enrol(participant, categoryId);
        return (organiser, participant, eventId, enrolmentId);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantSeesIt()
    {
        var (organiser, participant, eventId, enrolmentId) = await Arrange();

        var res = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:52:30", position = 7, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var mine = await (await participant.GetAsync("/api/results/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(7, mine[0].GetProperty("position").GetInt32());

        var forEvent = await Anonymous().GetAsync($"/api/events/{eventId}/results");
        Assert.Equal(HttpStatusCode.OK, forEvent.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (_, participant, _, enrolmentId) = await Arrange();
        var res = await participant.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithoutToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/results",
            new { enrolmentId = 1, finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent()
    {
        var (_, _, _, enrolmentId) = await Arrange();
        var otherOrganiser = await LoginAs("Organiser");
        var res = await otherOrganiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _, enrolmentId) = await Arrange();
        var body = new { enrolmentId, finishTime = "01:00:00", position = 3, status = "Finished" };
        await organiser.PostAsJsonAsync("/api/results", body);

        var second = await organiser.PostAsJsonAsync("/api/results", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await Arrange();
        var res = await organiser.PostAsJsonAsync("/api/results", new { enrolmentId, position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_DnfWithoutTime_IsAllowed()
    {
        var (organiser, _, _, enrolmentId) = await Arrange();
        var res = await organiser.PostAsJsonAsync("/api/results", new { enrolmentId, status = "DNF" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectResult_ParticipantCannot()
    {
        var (organiser, participant, _, enrolmentId) = await Arrange();
        var created = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "01:10:00", position = 9, status = "Finished" });
        var resultId = await IdOf(created, "resultID");

        var fix = await organiser.PutAsJsonAsync($"/api/results/{resultId}",
            new { finishTime = "01:05:00", position = 5, status = "Finished" });
        Assert.Equal(HttpStatusCode.OK, fix.StatusCode);
        Assert.Equal(5, (await fix.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("position").GetInt32());

        var denied = await participant.PutAsJsonAsync($"/api/results/{resultId}",
            new { finishTime = "00:30:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantResultHistory_Returns403()
    {
        var organiser = await LoginAs("Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/me")).StatusCode);
    }
}
