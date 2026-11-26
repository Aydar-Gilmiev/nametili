using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Expense_Id_Fk), nameof(User_Id_Fk), IsUnique = true)]
public class ExpenseShare
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExpenseShare_Id { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    [Required]
    [ForeignKey("Expense")]
    public int Expense_Id_Fk { get; set; }
    public Expense Expense { get; set; }

    [Required]
    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
