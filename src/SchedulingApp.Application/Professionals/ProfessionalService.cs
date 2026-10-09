using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Common;
using SchedulingApp.Contracts.Professionals;
using SchedulingApp.Domain.Common;

namespace SchedulingApp.Application.Professionals;

public sealed class ProfessionalService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ProfessionalDto>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Professionals.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.DisplayName.ToLower().Contains(term) || p.Specialty.ToLower().Contains(term));
        }

        var items = await query.OrderBy(p => p.DisplayName).Take(100).ToListAsync(ct);
        return items.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProfessionalDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var professional = await db.Professionals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Professional");
        return professional.ToDto();
    }

    public Task<ProfessionalDto> GetMineAsync(Actor actor, CancellationToken ct = default) =>
        GetAsync(actor.RequireProfessionalId(), ct);

    public async Task<ProfessionalDto> UpdateMineAsync(Actor actor, UpdateProfessionalProfileRequest request, CancellationToken ct = default)
    {
        var id = actor.RequireProfessionalId();
        var professional = await db.Professionals.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Professional");

        try
        {
            professional.UpdateProfile(request.DisplayName, request.Specialty, request.Bio, request.SlotDurationMinutes, request.TimeZoneId);
        }
        catch (DomainException ex)
        {
            throw ValidationException.For(ex.Code, ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return professional.ToDto();
    }
}
