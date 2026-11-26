using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Proposal_Id_Fk), IsUnique = true)]
public class Expense
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Expense_Id { get; set; }
    public string Title { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    public DateOnly Occurred_On { get; set; }

    [Required]
    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [Required]
    [ForeignKey("User")]
    public int Payer_User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }

    [ForeignKey("Proposal")]
    public int? Proposal_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public Proposal? Proposal { get; set; }
}
