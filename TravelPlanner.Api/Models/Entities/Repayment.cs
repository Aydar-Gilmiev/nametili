using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

public class Repayment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Repayment_Id { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    public DateOnly Paid_On { get; set; }

    [Required]
    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [Required]
    [ForeignKey("Sender")]
    public int From_User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User Sender { get; set; }

    [Required]
    [ForeignKey("User")]
    public int To_User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
