using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Trip_Id_Fk), nameof(Chat_Message_Id))]
public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Chat_Message_Id { get; set; }

    [Required, MaxLength(2000)]
    public string Text { get; set; }
    public DateTime Created_At { get; set; }

    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
