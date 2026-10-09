# One-time helper: removes the files of the previous prototype (RabbitMQ worker, Redis cache,
# MVC controllers, Blazor template pages) and records the new version as a sequence of small,
# conventional commits. Run from the repository root:
#
#   pwsh ./scripts/cleanup-and-commit.ps1            # dry run: only shows what would happen
#   pwsh ./scripts/cleanup-and-commit.ps1 -Apply     # really deletes and commits
#
# Review `git status` / `git log` afterwards, then `git push`.

param([switch]$Apply)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

function Run([string]$cmd) {
    Write-Host "> $cmd" -ForegroundColor Cyan
    if ($Apply) { Invoke-Expression $cmd; if ($LASTEXITCODE -ne 0) { throw "Command failed: $cmd" } }
}

# ---------------------------------------------------------------------------
# 0. GitHub files were delivered in "_github" (the remote tool cannot write .github)
# ---------------------------------------------------------------------------
if (Test-Path '_github') {
    Write-Host "move _github -> .github" -ForegroundColor Yellow
    if ($Apply) {
        New-Item -ItemType Directory -Force '.github' | Out-Null
        Copy-Item '_github/*' '.github' -Recurse -Force
        Remove-Item '_github' -Recurse -Force
    }
}

# ---------------------------------------------------------------------------
# 1. Files from the old prototype that no longer exist in the new design
# ---------------------------------------------------------------------------
$obsolete = @(
    'src/SchedulingApp.NotificationsWorker',
    'src/SchedulingApp.Api/Controllers',
    'src/SchedulingApp.Api/SchedulingApp.Api.http',
    'src/SchedulingApp.Application/Abstractions/INotificationPublisher.cs',
    'src/SchedulingApp.Application/Abstractions/ISlotCache.cs',
    'src/SchedulingApp.Application/Contracts',
    'src/SchedulingApp.Application/Dtos',
    'src/SchedulingApp.Application/Options',
    'src/SchedulingApp.Application/Services',
    'src/SchedulingApp.Domain/Constants',
    'src/SchedulingApp.Infrastructure/Auth',
    'src/SchedulingApp.Infrastructure/Caching',
    'src/SchedulingApp.Infrastructure/Data',
    'src/SchedulingApp.Infrastructure/Email/EmailSender.cs',
    'src/SchedulingApp.Infrastructure/Messaging',
    'src/SchedulingApp.Web/Components/Pages/AdminAvailability.razor',
    'src/SchedulingApp.Web/Components/Pages/AdminDashboard.razor',
    'src/SchedulingApp.Web/Components/Pages/Appointments.razor',
    'src/SchedulingApp.Web/Components/Pages/Book.razor',
    'src/SchedulingApp.Web/Components/Pages/Counter.razor',
    'src/SchedulingApp.Web/Components/Pages/Weather.razor',
    'src/SchedulingApp.Web/Components/Layout/MainLayout.razor.css',
    'src/SchedulingApp.Web/Components/Layout/NavMenu.razor',
    'src/SchedulingApp.Web/Components/Layout/NavMenu.razor.css',
    'src/SchedulingApp.Web/Services/AuthSession.cs',
    'src/SchedulingApp.Web/Services/JwtAuthenticationStateProvider.cs',
    'src/SchedulingApp.Web/Services/SchedulingApiClient.cs',
    'src/SchedulingApp.Web/wwwroot/lib',
    'src/SchedulingApp.Web/wwwroot/favicon.png',
    'tests/SchedulingApp.UnitTests/UnitTest1.cs',
    'tests/SchedulingApp.IntegrationTests/UnitTest1.cs'
)

foreach ($path in $obsolete) {
    if (Test-Path $path) {
        Write-Host "delete $path" -ForegroundColor Yellow
        if ($Apply) { Remove-Item $path -Recurse -Force }
    }
}

# Stop tracking build output that may have been committed before the .gitignore existed.
Run 'git rm -r -q --cached --ignore-unmatch -- "*/bin/*" "*/obj/*" ".vs"'

# ---------------------------------------------------------------------------
# 2. Small, focused commits
# ---------------------------------------------------------------------------
$commits = [ordered]@{
    'chore: add repository hygiene (.gitignore, editorconfig, MIT license, central package management)' =
        @('.gitignore', '.gitattributes', '.editorconfig', 'LICENSE', 'global.json', 'nuget.config',
          'Directory.Build.props', 'Directory.Packages.props', '.config', 'SchedulingApp.slnx')
    'refactor: drop RabbitMQ worker, Redis cache and template leftovers' =
        @('src/SchedulingApp.NotificationsWorker', 'src/SchedulingApp.Api/Controllers', 'src/SchedulingApp.Api/SchedulingApp.Api.http',
          'src/SchedulingApp.Application/Contracts', 'src/SchedulingApp.Application/Dtos', 'src/SchedulingApp.Application/Options',
          'src/SchedulingApp.Application/Services', 'src/SchedulingApp.Domain/Constants', 'src/SchedulingApp.Infrastructure/Auth',
          'src/SchedulingApp.Infrastructure/Caching', 'src/SchedulingApp.Infrastructure/Data', 'src/SchedulingApp.Infrastructure/Messaging',
          'src/SchedulingApp.Infrastructure/Email/EmailSender.cs', 'src/SchedulingApp.Web/wwwroot/lib', 'src/SchedulingApp.Web/wwwroot/favicon.png',
          'src/SchedulingApp.Application/Abstractions/INotificationPublisher.cs', 'src/SchedulingApp.Application/Abstractions/ISlotCache.cs',
          'tests/SchedulingApp.UnitTests/UnitTest1.cs', 'tests/SchedulingApp.IntegrationTests/UnitTest1.cs')
    'feat(domain): appointments lifecycle, availability rules and time-zone aware slot calculator' =
        @('src/SchedulingApp.Domain')
    'feat(contracts): shared HTTP contracts for API and front-end' =
        @('src/SchedulingApp.Contracts')
    'feat(application): booking, availability and professional use cases with email outbox' =
        @('src/SchedulingApp.Application')
    'feat(infrastructure): EF Core PostgreSQL, Identity + JWT, outbox dispatcher and email senders' =
        @('src/SchedulingApp.Infrastructure')
    'feat(api): minimal API endpoints, ProblemDetails, OpenAPI, rate limiting and health checks' =
        @('src/SchedulingApp.Api/Program.cs', 'src/SchedulingApp.Api/Endpoints', 'src/SchedulingApp.Api/Infrastructure',
          'src/SchedulingApp.Api/Properties', 'src/SchedulingApp.Api/appsettings.json', 'src/SchedulingApp.Api/appsettings.Development.json',
          'src/SchedulingApp.Api/SchedulingApp.Api.csproj')
    'feat(web): Blazor UI for booking, client appointments, professional schedule and availability' =
        @('src/SchedulingApp.Web/Program.cs', 'src/SchedulingApp.Web/Components', 'src/SchedulingApp.Web/Services',
          'src/SchedulingApp.Web/wwwroot', 'src/SchedulingApp.Web/Properties', 'src/SchedulingApp.Web/appsettings.json',
          'src/SchedulingApp.Web/appsettings.Development.json', 'src/SchedulingApp.Web/SchedulingApp.Web.csproj')
    'test: unit tests for domain rules and use cases' =
        @('tests/SchedulingApp.UnitTests')
    'test: integration tests with Testcontainers PostgreSQL' =
        @('tests/SchedulingApp.IntegrationTests')
    'test: Playwright end-to-end scenarios' =
        @('tests/SchedulingApp.E2ETests')
    'build: Dockerfiles, docker-compose with PostgreSQL and Mailpit, .env.example' =
        @('src/SchedulingApp.Api/Dockerfile', 'src/SchedulingApp.Web/Dockerfile', '.dockerignore', 'docker-compose.yml', '.env.example')
    'ci: GitHub Actions pipeline (build, unit, integration, E2E, docker) and Render blueprint' =
        @('.github', 'render.yaml')
    'docs: README, architecture, screens and roadmap' =
        @('README.md', 'ROADMAP.md', 'docs', 'scripts')
}

foreach ($entry in $commits.GetEnumerator()) {
    # keep only paths that exist on disk or are still tracked (deleted files)
    $existing = $entry.Value | Where-Object { (Test-Path $_) -or (git ls-files -- $_) }
    if (-not $existing) { continue }
    $paths = ($existing | ForEach-Object { "`"$_`"" }) -join ' '
    Run "git add -A -- $paths"
    if ($Apply) {
        git diff --cached --quiet
        if ($LASTEXITCODE -eq 0) { Write-Host "  (nothing to commit)"; continue }
    }
    Run "git commit -q -m `"$($entry.Key)`""
}

Run 'git add -A'
if ($Apply) {
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) { Run 'git commit -q -m "chore: remaining files"' }
    git log --oneline -n 20
} else {
    Write-Host "`nDry run only. Re-run with -Apply to execute." -ForegroundColor Green
}
