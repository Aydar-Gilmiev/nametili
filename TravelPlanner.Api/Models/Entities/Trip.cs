using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

public class Trip
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Trip_Id { get; set; }

    public string Trip_Name { get; set; }
    public string Destination { get; set; }

    public DateOnly Start_Date { get; set; }

    public DateOnly End_Date { get; set; }

    [Required]
    [ForeignKey("User")]
    public int Owner_User_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
