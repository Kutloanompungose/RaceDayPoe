using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Web.Tests;

/// <summary>Proves protected pages are unreachable without a session (no API needed - the filter redirects first).</summary>
public class AccessControlTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient NewClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/Organiser/Dashboard")]
    [InlineData("/Organiser/Events")]
    [InlineData("/Organiser/CreateEvent")]
    [InlineData("/Participant/Events")]
    [InlineData("/Participant/MyEnrolments")]
    [InlineData("/Participant/MyResults")]
    [InlineData("/Participant/Profile")]
    public async Task ProtectedPages_RedirectAnonymousVisitorsToLogin(string url)
    {
        var response = await NewClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    public async Task PublicPages_AreReachable(string url)
    {
        var response = await NewClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPage_ShowsFriendlyNotFoundPage()
    {
        var response = await NewClient().GetAsync("/this/does/not/exist");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("page not found", html, StringComparison.OrdinalIgnoreCase);
    }
}
