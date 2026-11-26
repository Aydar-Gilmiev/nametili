using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

public class Proposal
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Proposal_Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    [Precision(12, 2)]
    public decimal Planned_Cost { get; set; }
    public string Status { get; set; }

    public bool Voting_Closed { get; set; }

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
