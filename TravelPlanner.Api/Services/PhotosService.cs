using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.UniversalMethod;
using TravelPlanner.Api.Interfaces;
using System.Data;
using Npgsql;

namespace TravelPlanner.Api.Services;

public class PhotosService : IPhotosService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;
    public PhotosService(ContextDb db, TripAccess access) { _db = db; _access = access; }

    public async Task<IActionResult> Get(int tripId, int? beforeId)
    {
        if (beforeId <= 0) return new BadRequestResult();
        return new OkObjectResult(await _db.TripPhotos.Where(value => value.Trip_Id_Fk == tripId
                && (!beforeId.HasValue || value.Trip_Photo_Id < beforeId.Value))
            .OrderByDescending(value => value.Trip_Photo_Id).Take(12)
            .Select(value => new { Id = value.Trip_Photo_Id, UserId = value.User_Id_Fk, Name = value.User.User_Name,
                value.Caption, CreatedAt = value.Created_At }).ToListAsync());
    }

    public async Task<IActionResult> Content(int tripId, int id)
    {
        var photo = await _db.TripPhotos.AsNoTracking().Where(value => value.Trip_Id_Fk == tripId && value.Trip_Photo_Id == id)
            .Select(value => new { value.Image_Data, value.Content_Type }).SingleOrDefaultAsync();
        return photo == null ? new NotFoundResult() : new FileContentResult(photo.Image_Data, photo.Content_Type);
    }

    public async Task<IActionResult> Upload(int tripId, IFormFile file, string? caption, int userId)
    {
        if (file.Length is < 1 or > PhotoHelper.MaxBytes || (caption?.Length ?? 0) > 300)
            return new BadRequestObjectResult("Выберите JPEG или PNG до 5 МБ и подпись до 300 символов.");
        using var memory = new MemoryStream();
        await using var input = file.OpenReadStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            if (memory.Length + read > PhotoHelper.MaxBytes) return new BadRequestObjectResult("Файл больше 5 МБ.");
            await memory.WriteAsync(buffer.AsMemory(0, read));
        }
        var data = memory.ToArray();
        var type = PhotoHelper.ContentType(data);
        if (type == null) return new BadRequestObjectResult("Нужна фотография JPEG или PNG до 20 мегапикселей.");
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var found = await _db.Trips.Where(value => value.Trip_Id == tripId)
                .ExecuteUpdateAsync(fields => fields.SetProperty(value => value.Trip_Name, value => value.Trip_Name));
            if (found == 0) return new NotFoundResult();
            if (await _db.TripPhotos.CountAsync(value => value.Trip_Id_Fk == tripId) >= 100)
                return new ConflictObjectResult("В поездке уже 100 фотографий. Удалите ненужные.");
            var photo = new TripPhoto { Trip_Id_Fk = tripId, User_Id_Fk = userId, Caption = (caption ?? "").Trim(),
                Content_Type = type, Image_Data = data, Created_At = DateTime.UtcNow };
            _db.TripPhotos.Add(photo);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new OkObjectResult(new { Id = photo.Trip_Photo_Id });
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" })
        { return new ConflictObjectResult("Альбом изменился одновременно. Обновите его и повторите загрузку."); }
    }

    public async Task<IActionResult> Delete(int tripId, int id, int userId)
    {
        var photo = await _db.TripPhotos.SingleOrDefaultAsync(value => value.Trip_Photo_Id == id && value.Trip_Id_Fk == tripId);
        if (photo == null) return new NotFoundResult();
        if (photo.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        _db.TripPhotos.Remove(photo);
        await _db.SaveChangesAsync();
        return new OkResult();
    }
}
