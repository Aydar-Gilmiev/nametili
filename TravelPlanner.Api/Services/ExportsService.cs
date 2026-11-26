using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.UniversalMethod;

namespace TravelPlanner.Api.Services;

public class ExportsService : IExportsService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;
    public ExportsService(ContextDb db, TripAccess access) { _db = db; _access = access; }

    public async Task<IActionResult> PdfAsync(int tripId, int userId)
    {
        if (!await _access.IsMember(tripId, userId)) return new StatusCodeResult(403);
        var trip = await _db.Trips.AsNoTracking().SingleAsync(value => value.Trip_Id == tripId);
        var items = await _db.ScheduleItems.AsNoTracking().Include(value => value.Proposal)
            .Where(value => value.Trip_Id_Fk == tripId).OrderBy(value => value.Start_At).ThenBy(value => value.ScheduleItem_Id).ToListAsync();
        var places = await _db.TripPlaces.AsNoTracking().Where(value => value.Trip_Id_Fk == tripId).OrderBy(value => value.Trip_Place_Id).ToListAsync();
        var proposals = await _db.Proposals.AsNoTracking().Where(value => value.Trip_Id_Fk == tripId && value.Status == "Approved").ToListAsync();
        return new FileContentResult(TripPdfHelper.Create(trip, items, places, proposals), "application/pdf") { FileDownloadName = $"trip-{tripId}.pdf" };
    }

}
