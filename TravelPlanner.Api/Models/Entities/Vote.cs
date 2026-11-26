using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Proposal_Id_Fk), nameof(User_Id_Fk), IsUnique = true)]
public class Vote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Vote_Id { get; set; }
    public int Value { get; set; }

    [Required]
    [ForeignKey("Proposal")]
    public int Proposal_Id_Fk { get; set; }
    public Proposal Proposal { get; set; }

    [Required]
    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
