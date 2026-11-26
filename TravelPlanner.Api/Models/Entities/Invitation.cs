using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Trip_Id_Fk), nameof(Email), IsUnique = true)]
public class Invitation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Invitation_Id { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Status { get; set; }

    public DateTime Created_At { get; set; }

    [Required]
    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }
}
