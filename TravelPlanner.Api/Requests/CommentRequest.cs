using System.ComponentModel.DataAnnotations;

namespace TravelPlanner.Api.Requests;

public class CommentRequest
{
    [Required, StringLength(2000)]
    public string Text { get; set; } = "";
}
