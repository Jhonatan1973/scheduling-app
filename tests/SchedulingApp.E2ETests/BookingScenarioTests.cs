using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SchedulingApp.E2ETests;

[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public class BookingScenarioTests(PlaywrightFixture fixture)
{
    [E2EFact]
    public async Task Professional_publishes_hours_client_books_and_professional_confirms()
    {
        var (context, page) = await fixture.NewPageAsync(nameof(Professional_publishes_hours_client_books_and_professional_confirms));
        try
        {
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var proName = $"E2E Pro {suffix}";
            var proEmail = Pages.UniqueEmail("pro");
            var clientName = $"E2E Client {suffix}";

            // ---- Professional signs up and publishes working hours for every day ----
            await Pages.RegisterAsync(page, proName, proEmail, professional: true, specialty: "Barber");
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/pro/availability"));

            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                await page.GetByTestId("rule-day").SelectOptionAsync(day);
                await page.GetByTestId("rule-start").FillAsync("00:00");
                await page.GetByTestId("rule-end").FillAsync("23:30");
                await page.GetByTestId("rule-add").ClickAsync();
                await Expect(page.GetByTestId("alert-success").Filter(new() { HasText = day })).ToBeVisibleAsync();
            }
            await Expect(page.GetByTestId("rule-item")).ToHaveCountAsync(7);
            await Pages.SignOutAsync(page);

            // ---- Client signs up, finds the professional and books the first free slot ----
            await Pages.RegisterAsync(page, clientName, Pages.UniqueEmail("client"), professional: false);
            await page.GetByPlaceholder("Search by name or specialty…").FillAsync(proName);
            await page.GetByPlaceholder("Search by name or specialty…").PressAsync("Enter");
            await page.GetByTestId("professional-card").Filter(new() { HasText = proName }).ClickAsync();

            await PickFirstFreeSlotAsync(page);
            await page.GetByTestId("booking-notes").FillAsync("Booked by Playwright");
            await page.GetByTestId("book-submit").ClickAsync();
            await Expect(page.GetByTestId("booking-success")).ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Link, new() { Name = "View my appointments" }).ClickAsync();
            var item = page.GetByTestId("appointment-item").Filter(new() { HasText = proName });
            await Expect(item.GetByTestId("status-badge")).ToHaveTextAsync("Pending");
            await Pages.SignOutAsync(page);

            // ---- Professional confirms the request from the schedule ----
            await Pages.LoginAsync(page, proEmail);
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/pro/schedule"));

            var request = page.GetByTestId("schedule-item").Filter(new() { HasText = clientName });
            for (var week = 0; week < 4 && await request.CountAsync() == 0; week++)
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
                await page.WaitForTimeoutAsync(500);
            }

            await request.GetByTestId("confirm-appointment").ClickAsync();
            await Expect(request.GetByTestId("status-badge")).ToHaveTextAsync("Confirmed");
            await Expect(page.GetByTestId("alert-success").Filter(new() { HasText = "Confirmed" })).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightFixture.CloseAsync(context, nameof(Professional_publishes_hours_client_books_and_professional_confirms));
        }
    }

    [E2EFact]
    public async Task Anonymous_visitor_is_asked_to_sign_in_before_booking()
    {
        var (context, page) = await fixture.NewPageAsync(nameof(Anonymous_visitor_is_asked_to_sign_in_before_booking));
        try
        {
            await page.GotoAsync("/");
            await page.GetByTestId("professional-card").First.ClickAsync();

            await PickFirstFreeSlotAsync(page);

            await page.GetByTestId("login-to-book").ClickAsync();
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/login\\?returnUrl="));
        }
        finally
        {
            await PlaywrightFixture.CloseAsync(context, nameof(Anonymous_visitor_is_asked_to_sign_in_before_booking));
        }
    }

    [E2EFact]
    public async Task Layout_is_usable_on_a_phone()
    {
        var (context, page) = await fixture.NewPageAsync(nameof(Layout_is_usable_on_a_phone), new ViewportSize { Width = 390, Height = 844 });
        try
        {
            await page.GotoAsync("/");

            var findLink = page.GetByRole(AriaRole.Link, new() { Name = "Find a professional" }).First;
            var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation" });

            await Expect(toggle).ToBeVisibleAsync();
            await toggle.ClickAsync();
            await Expect(page.Locator("nav.nav.open")).ToBeVisibleAsync();

            // No horizontal scrolling on small screens.
            var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth");
            Assert.False(overflow);
            await Expect(findLink).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightFixture.CloseAsync(context, nameof(Layout_is_usable_on_a_phone));
        }
    }

    /// <summary>Walks the date strip until a day with a free slot is found and selects it.</summary>
    private static async Task PickFirstFreeSlotAsync(IPage page)
    {
        var dates = page.GetByTestId("date-chip").And(page.Locator(":not([disabled])"));
        await Expect(dates.First).ToBeVisibleAsync();
        var count = await dates.CountAsync();

        for (var i = 0; i < count; i++)
        {
            await dates.Nth(i).ClickAsync();
            await page.WaitForTimeoutAsync(400);

            var freeSlot = page.Locator("[data-testid=slot]:not([disabled])").First;
            if (await freeSlot.CountAsync() > 0)
            {
                await freeSlot.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException("No free slot found in the date strip.");
    }
}
