using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

public class AuthTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_WithValidDetails_Returns201(string role)
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane Runner", email = NewEmail(), password = Password, role });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(role, json.GetProperty("role").GetString());
        Assert.False(json.TryGetProperty("passwordHash", out _)); // never leak the hash
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = NewEmail();
        var body = new { fullName = "Jane", email, password = Password, role = "Participant" };
        await Anonymous().PostAsJsonAsync("/api/auth/register", body);

        var second = await Anonymous().PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane", email = NewEmail(), password = Password, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane", email = NewEmail(), password = "short", role = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndUser()
    {
        var email = NewEmail();
        await LoginAs("Participant", email);

        var res = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
        Assert.Equal("Participant", json.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = NewEmail();
        await LoginAs("Participant", email);

        var res = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutToken_Returns401()
    {
        var res = await Anonymous().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_CanBeViewedAndUpdated_ByBothRoles()
    {
        foreach (var role in new[] { "Organiser", "Participant" })
        {
            var client = await LoginAs(role);

            var get = await client.GetAsync("/api/users/me");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            var put = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", phone = "0821234567" });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            var json = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Updated Name", json.GetProperty("fullName").GetString());
            Assert.Equal("0821234567", json.GetProperty("phone").GetString());
        }
    }

    [Fact]
    public void TokenService_CreatesTokenContainingRoleAndUserId()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "unit-test-key-that-is-at-least-32-characters-long!",
            ["Jwt:Issuer"] = "issuer", ["Jwt:Audience"] = "aud", ["Jwt:ExpiryMinutes"] = "5"
        }).Build();

        var (token, expires) = new TokenService(config).Create(
            new User { UserID = 42, Email = "a@b.com", FullName = "A B", Role = Roles.Organiser });

        var parsed = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("42", parsed.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("Organiser", parsed.Claims.First(c => c.Type == "role").Value);
        Assert.True(expires > DateTime.UtcNow);
    }
}
