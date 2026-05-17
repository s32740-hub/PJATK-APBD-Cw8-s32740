using zd8.DTOs;

namespace zd8.Service;

public interface IPatientsService
{
    Task<IEnumerable<PatientResponse>> GetPatientsAsync(string? search, CancellationToken cancellationToken);
    Task AssignBedAsync(string pesel, AssignBedRequest request, CancellationToken cancellationToken);
}