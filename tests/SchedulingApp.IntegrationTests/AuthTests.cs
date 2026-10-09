using System.Net;
using System.Net.Http.Json;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Auth;
using static SchedulingApp.IntegrationTests.TestApi;

namespace SchedulingApp.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Registers_a_client_and_logs_in()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail("login");

        var register = await client.PostAsJsonAsync("api/auth/register",
            new RegisterRequest("Ana Client", email, "Passw0rd!", UserRole.Client), Json);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "Passw0rd!"), Json);
        var auth = await login.ReadAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(UserRole.Client, auth.User.Role);
        Assert.Null(auth.User.ProfessionalId);
    }

    [Fact]
    public async Task Registering_a_professional_creates_the_professional_profile()
    {
        var (client, auth) = await RegisterAsync(factory, UserRole.Professional, "Bruno Barber");

        Assert.NotNull(auth.User.ProfessionalId);
        var me = await client.GetAsync("api/auth/me");
        Assert.Equal(UserRole.Professional, (await me.ReadAsync<UserDto>()).Role);

        var list = await factory.CreateClient().GetStringAsync("api/professionals?search=bruno");
        Assert.Contains("Bruno Barber", list);
    }

    [Fact]
    public async Task Rejects_wrong_password()
    {
        var (_, auth) = await RegisterAsync(factory, UserRole.Client);

        var login = await factory.CreateClient().PostAsJsonAsync("api/auth/login", new LoginRequest(auth.User.Email, "wrong-password1"), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Rejects_duplicate_email_with_conflict()
    {
        var (_, auth) = await RegisterAsync(factory, UserRole.Client);

        var again = await factory.CreateClient().PostAsJsonAsync("api/auth/register",
            new RegisterRequest("Again", auth.User.Email, "Passw0rd!", UserRole.Client), Json);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Returns_validation_problem_details_for_invalid_input()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("api/auth/register",
            new RegisterRequest("", "not-an-email", "123", UserRole.Professional), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("email", body);
        Assert.Contains("specialty", body);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
    {
        var response = await factory.CreateClient().GetAsync("api/appointments/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
