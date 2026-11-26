using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Services;

public class PlacesService : IPlacesService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;
    public PlacesService(ContextDb db, TripAccess access) { _db = db; _access = access; }
    public async Task<IActionResult> Get(int tripId) => new OkObjectResult(await _db.TripPlaces.Where(value => value.Trip_Id_Fk == tripId)
        .OrderBy(value => value.Trip_Place_Id).Select(value => new { Id = value.Trip_Place_Id, UserId = value.User_Id_Fk,
            value.Title, value.Note, value.Latitude, value.Longitude }).ToListAsync());

    public async Task<IActionResult> Save(int tripId, int? id, PlaceRequest request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 || (request.Note?.Length ?? 0) > 1000
            || !double.IsFinite(request.Latitude) || !double.IsFinite(request.Longitude)
            || request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return new BadRequestObjectResult("Проверьте название, заметку и координаты.");
        var place = id.HasValue
            ? await _db.TripPlaces.SingleOrDefaultAsync(value => value.Trip_Place_Id == id && value.Trip_Id_Fk == tripId)
            : new TripPlace { Trip_Id_Fk = tripId, User_Id_Fk = userId };
        if (place == null) return new NotFoundResult();
        if (place.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        place.Title = request.Title.Trim();
        place.Note = (request.Note ?? "").Trim();
        place.Latitude = request.Latitude;
        place.Longitude = request.Longitude;
        if (!id.HasValue) _db.TripPlaces.Add(place);
        await _db.SaveChangesAsync();
        return new OkObjectResult(new { Id = place.Trip_Place_Id });
    }

    public async Task<IActionResult> Delete(int tripId, int id, int userId)
    {
        var place = await _db.TripPlaces.SingleOrDefaultAsync(value => value.Trip_Place_Id == id && value.Trip_Id_Fk == tripId);
        if (place == null) return new NotFoundResult();
        if (place.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        _db.TripPlaces.Remove(place);
        await _db.SaveChangesAsync();
        return new OkResult();
    }
}
