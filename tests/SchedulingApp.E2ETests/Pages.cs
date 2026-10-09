using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SchedulingApp.E2ETests;

/// <summary>Small page-object helpers shared by the scenarios.</summary>
internal static class Pages
{
    public const string Password = "Passw0rd!e2e";

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@e2e.test";

    public static async Task RegisterAsync(IPage page, string name, string email, bool professional, string? specialty = null)
    {
        await page.GotoAsync("/register");
        if (professional)
            await page.GetByTestId("role-professional").ClickAsync();

        await page.GetByTestId("register-name").FillAsync(name);
        await page.GetByTestId("register-email").FillAsync(email);
        await page.GetByTestId("register-password").FillAsync(Password);
        if (professional)
            await page.GetByTestId("register-specialty").FillAsync(specialty ?? "Barber");

        await page.GetByTestId("register-submit").ClickAsync();
        await Expect(page.GetByTestId("sign-out")).ToBeVisibleAsync();
    }

    public static async Task LoginAsync(IPage page, string email, string password = Password)
    {
        await page.GotoAsync("/login");
        await page.GetByTestId("login-email").FillAsync(email);
        await page.GetByTestId("login-password").FillAsync(password);
        await page.GetByTestId("login-submit").ClickAsync();
        await Expect(page.GetByTestId("sign-out")).ToBeVisibleAsync();
    }

    public static async Task SignOutAsync(IPage page)
    {
        await page.GetByTestId("sign-out").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();
    }
}
