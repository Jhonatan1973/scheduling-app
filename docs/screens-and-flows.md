# Screens & user flows

## Profiles

| Profile | Can |
|---|---|
| **Visitor** | Browse/search professionals, see working hours and free slots |
| **Client** | Everything above + request appointments, see upcoming/past ones, cancel |
| **Professional** | Publish weekly hours, edit public profile, see day/week agenda, confirm/decline/cancel |

## Flows

```mermaid
flowchart TD
    A[Home: list & search professionals] --> B[Professional page]
    B --> C{Pick date in strip}
    C --> D[Pick free slot]
    D --> E{Signed in as client?}
    E -- no --> F[Sign in / Create account] --> B
    E -- yes --> G[Add notes · Request appointment]
    G --> H[Success panel · email 'request received']
    H --> I[My appointments: Pending]

    P[Professional: Schedule] --> Q{Pending request}
    Q -- Confirm --> R[Confirmed · email to client]
    Q -- Decline --> S[Cancelled · email to client]
    I -- Cancel --> T[Cancelled · email to professional · slot free again]
```

```mermaid
flowchart LR
    R1[Register as professional] --> R2[Availability page<br/>welcome hint] --> R3[Add intervals per weekday] --> R4[Set session length & time zone] --> R5[Share public page]
```

## Wireframes

### Home (`/`)
```
┌────────────────────────────────────────────────────────────────┐
│ ◷ SchedulingApp   Find a professional          Sign in [Create]│
├────────────────────────────────────────────────────────────────┤
│ ┌─────────────── gradient hero ───────────────┬──────────────┐ │
│ │ Book your next appointment in seconds.      │ 1 Choose     │ │
│ │ [Find a professional] [I'm a professional]  │ 2 Pick slot  │ │
│ │                                             │ 3 Confirmed  │ │
│ └─────────────────────────────────────────────┴──────────────┘ │
│ Professionals                     [ search…            ][Go]   │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐                         │
│ │ (S)      │ │ (L)      │ │ (E)      │   cards: name, specialty│
│ │ Sofia    │ │ Lucas    │ │ Emma     │   bio, session length,  │
│ │ Derm.    │ │ Barber   │ │ Tutor    │   time zone, CTA        │
│ └──────────┘ └──────────┘ └──────────┘                         │
└────────────────────────────────────────────────────────────────┘
```

### Professional page / booking (`/professionals/{id}`)
```
┌───────────────┬────────────────────────────────────────────────┐
│ (avatar)      │ Choose a date                                  │
│ Dr. Sofia     │ [Mon 12][Tue 13][Wed 14][Thu 15][Fri 16][Sat]→ │
│ Dermatologist │ Available times (America/Sao Paulo)            │
│ bio…          │ [09:00][09:30][1̶0̶:̶0̶0̶][10:30][11:00] …         │
│ Session 30min │ ───────────────────────────────────────────    │
│ Working hours │ Tuesday, Oct 13, 09:30 – 10:00                 │
│ Mon 09–12,…   │ Notes [__________________]                     │
│               │ [ Request appointment ]                        │
└───────────────┴────────────────────────────────────────────────┘
```

### My appointments (`/my/appointments`, client)
```
My appointments                                      [Book new]
[ Upcoming (2) | Past & cancelled (1) ]
┌──────┬──────────────────────────────────────────┬──────────┐
│ OCT  │ Dr. Sofia Martins  (PENDING)             │ [Cancel] │
│ 13   │ Dermatologist · 09:30–10:00 (Sao Paulo)  │          │
│ TUE  │ "First visit"                            │          │
└──────┴──────────────────────────────────────────┴──────────┘
```

### Schedule (`/pro/schedule`, professional)
```
My schedule                         [Day|Week]  [‹][Today][›]
Oct 12 – Oct 18, 2026 · America/Sao Paulo
┌─────────┐┌───────────┐┌──────────┐
│ 2 Pend. ││ 5 Confirm.││ 1 Cancel.│        [ ] Show cancelled
└─────────┘└───────────┘└──────────┘
Monday, October 12  (Today)
│ 09:00 │ Carla Client (PENDING) carla@…  "First visit" │ [Confirm][Decline]
│ 10:30 │ Otto Other (CONFIRMED)                         │ [Cancel]
Tuesday, October 13
  No appointments.
```

### Availability (`/pro/availability`, professional)
```
Availability & profile                       [View public page ↗]
┌──────── Weekly hours ────────────┐ ┌──── Public profile ─────┐
│ Day[Mon▾] From[09:00] To[12:00]  │ │ Display name [_______]  │
│                          [Add]   │ │ Specialty    [_______]  │
│ Monday    (09:00–12:00 ✕)(13–18✕)│ │ Bio          [_______]  │
│ Tuesday   Unavailable            │ │ Session [30▾] TZ [SP▾]  │
│ …                                │ │ [Save profile]          │
└──────────────────────────────────┘ └─────────────────────────┘
```

On phones (< 760 px) the navigation collapses into a hamburger menu, the booking page stacks
profile above the calendar, and list items wrap their action buttons below the content.
