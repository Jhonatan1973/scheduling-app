# Roadmap

## Done (v1)
- [x] Client / Professional accounts (Identity + JWT)
- [x] Weekly availability with multiple intervals, session length and time zone
- [x] Free-slot listing and booking with double-booking protection
- [x] Confirm / decline / cancel with reasons
- [x] Email notifications through a transactional outbox
- [x] Professional day/week agenda
- [x] Responsive UI
- [x] Unit, integration (Testcontainers) and E2E (Playwright) tests
- [x] Docker, CI, Render Blueprint

## Next
- [ ] Reminder email 24 h before the appointment (scheduled job on the outbox)
- [ ] Days off / holidays (date-specific blocks on top of weekly hours)
- [ ] Multiple services per professional (name, price, duration)
- [ ] Email confirmation and password reset
- [ ] `.ics` calendar attachment / "Add to Google Calendar" link
- [ ] Refresh tokens and httpOnly cookie session for the web app
- [ ] Minimum notice and cancellation policy per professional (e.g. no cancellation < 2 h)
- [ ] Observability: OpenTelemetry traces + structured logs
- [ ] Localization (pt-BR / en-US)
- [ ] Dark mode
