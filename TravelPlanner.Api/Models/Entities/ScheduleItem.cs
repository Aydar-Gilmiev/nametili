using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Trip_Id_Fk), nameof(Start_At))]
public class ScheduleItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleItem_Id { get; set; }
    public string Title { get; set; }

    public DateTime Start_At { get; set; }

    public DateTime End_At { get; set; }

    [Required]
    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [ForeignKey("Proposal")]
    public int? Proposal_Id_Fk { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public Proposal? Proposal { get; set; }
}
