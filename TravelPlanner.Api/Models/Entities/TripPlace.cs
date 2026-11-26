using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TravelPlanner.Api.Models.Entities;

public class TripPlace
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Trip_Place_Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; }
    [MaxLength(1000)]
    public string Note { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
