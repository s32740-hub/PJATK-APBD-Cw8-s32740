using Microsoft.EntityFrameworkCore;
using zd8.DTOs;
using zd8.Exceptions;
using zd8.Infrastructure;
using zd8.Models;

namespace zd8.Service;

public class PatientsService(MasterContext db) : IPatientsService
{
    public async Task<IEnumerable<PatientResponse>> GetPatientsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, $"%{search}%") ||
                EF.Functions.Like(p.LastName, $"%{search}%"));
        }
        return await query.Select(p=>new PatientResponse
        {
            Pesel = p.Pesel,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Age = p.Age,
            Sex = p.Sex
        }).ToListAsync(cancellationToken);
    }

    public async Task AssignBedAsync(string pesel, AssignBedRequest request, CancellationToken cancellationToken)
    {
        var patientExists = await db.Patients.AnyAsync(p => p.Pesel == pesel, cancellationToken);
        if (!patientExists)
        {
            throw new NotFoundException($"Pacjent o numerze PESEL {pesel} nie istnieje.");
        }
        var availableBed = await db.Beds
            .Where(b=>b.BedTypeId==request.BedTypeId && b.Room.WardId == request.WardId)
            .Where(b=>!db.BedAssignments.Any(ba=>
                ba.BedId == b.Id &&
                ba.From <(request.To ?? DateTime.MaxValue)
                && (ba.To==null || ba.To>request.From))).FirstOrDefaultAsync(cancellationToken);
        if (availableBed == null)
        {
            throw new NotFoundException(
                "Brak wolnych łóżek o wskazanym typie na tym oddziale w podanym okresie czasu.");
        }
        var newAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        db.BedAssignments.Add(newAssignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}