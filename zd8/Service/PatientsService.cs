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
        return await query
            .Include(p => p.Admissions).ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.BedTypeNavigation)
            .Include(p=>p.BedAssignments)
            .ThenInclude(ba=>ba.Bed).ThenInclude(b=>b.Room).ThenInclude(r=>r.Ward)
            .Select(p => new PatientResponse
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",
                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),
                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedTypeNavigation.Id,
                            Name = ba.Bed.BedTypeNavigation.Name,
                            Description = ba.Bed.BedTypeNavigation.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                        }
            }).ToList()
                    }).ToListAsync(cancellationToken);
    }

    public async Task AssignBedAsync(string pesel, AssignBedRequest request, CancellationToken cancellationToken)
    {
        var patientExists = await db.Patients
            .AnyAsync(p => p.Pesel.Trim() == pesel.Trim(), cancellationToken);
        if (!patientExists)
        {
            throw new NotFoundException($"Pacjent o numerze PESEL {pesel} nie istnieje.");
        }
        var availableBed = await db.Beds
            .Where(b => b.BedTypeId == request.BedTypeId && b.Room.WardId == request.WardId)
            .Where(b => !db.BedAssignments.Any(ba =>
                ba.BedId == b.Id &&
                (request.To == null || ba.From < request.To)
                && (ba.To == null || ba.To > request.From)))
            .FirstOrDefaultAsync(cancellationToken);
        if (availableBed == null)
        {
            throw new NotFoundException(
                "Brak wolnych łóżek o wskazanym typie na tym oddziale w podanym okresie czasu.");
        }
        var newAssignment = new BedAssignment
        {
            PatientPesel = pesel.Trim(),
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        db.BedAssignments.Add(newAssignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}