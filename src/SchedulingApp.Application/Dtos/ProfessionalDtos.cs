namespace SchedulingApp.Application.Dtos;

public record ProfessionalDto(Guid Id, string DisplayName, string Specialty, int SlotDurationMinutes);
