using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Proposal_Id_Fk), nameof(Proposal_Comment_Id))]
public class ProposalComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Proposal_Comment_Id { get; set; }

    [Required, MaxLength(2000)]
    public string Text { get; set; }
    public DateTime Created_At { get; set; }

    [ForeignKey("Proposal")]
    public int Proposal_Id_Fk { get; set; }
    public Proposal Proposal { get; set; }

    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
