using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Services;

public class CommentsService : ICommentsService
{
    private readonly ContextDb _db;
    private readonly TripAccess _access;
    public CommentsService(ContextDb db, TripAccess access) { _db = db; _access = access; }

    public async Task<IActionResult> Get(int tripId, int proposalId, int? beforeId)
    {
        if (beforeId <= 0) return new BadRequestResult();
        if (!await _db.Proposals.AnyAsync(value => value.Proposal_Id == proposalId && value.Trip_Id_Fk == tripId))
            return new NotFoundResult();
        var comments = await _db.ProposalComments.Where(value => value.Proposal_Id_Fk == proposalId
                && (!beforeId.HasValue || value.Proposal_Comment_Id < beforeId.Value))
            .OrderByDescending(value => value.Proposal_Comment_Id).Take(50)
            .Select(value => new { Id = value.Proposal_Comment_Id, UserId = value.User_Id_Fk,
                Name = value.User.User_Name, value.Text, CreatedAt = value.Created_At }).ToListAsync();
        comments.Reverse();
        return new OkObjectResult(comments);
    }

    public async Task<IActionResult> Save(int tripId, int proposalId, int? id, CommentRequest request, int userId)
    {
        var text = (request.Text ?? "").Trim();
        if (text.Length is < 1 or > 2000) return new BadRequestObjectResult("Введите комментарий до 2000 символов.");
        if (!await _db.Proposals.AnyAsync(value => value.Proposal_Id == proposalId && value.Trip_Id_Fk == tripId))
            return new NotFoundResult();
        var comment = id.HasValue
            ? await _db.ProposalComments.SingleOrDefaultAsync(value => value.Proposal_Comment_Id == id && value.Proposal_Id_Fk == proposalId)
            : new ProposalComment { Proposal_Id_Fk = proposalId, User_Id_Fk = userId, Created_At = DateTime.UtcNow };
        if (comment == null) return new NotFoundResult();
        if (comment.User_Id_Fk != userId) return new StatusCodeResult(403);
        comment.Text = text;
        if (!id.HasValue) _db.ProposalComments.Add(comment);
        await _db.SaveChangesAsync();
        return new OkObjectResult(new { Id = comment.Proposal_Comment_Id });
    }

    public async Task<IActionResult> Delete(int tripId, int proposalId, int id, int userId)
    {
        var comment = await _db.ProposalComments.SingleOrDefaultAsync(value => value.Proposal_Comment_Id == id
            && value.Proposal_Id_Fk == proposalId && value.Proposal.Trip_Id_Fk == tripId);
        if (comment == null) return new NotFoundResult();
        if (comment.User_Id_Fk != userId && !await _access.IsOrganizer(tripId, userId)) return new StatusCodeResult(403);
        _db.ProposalComments.Remove(comment);
        await _db.SaveChangesAsync();
        return new OkResult();
    }
}
