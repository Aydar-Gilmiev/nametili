using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Trip_Id_Fk), nameof(User_Id_Fk), IsUnique = true)]
public class TripMember
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TripMember_Id { get; set; }
    public string Role { get; set; }

    [Required]
    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [Required]
    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
