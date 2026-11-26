using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TravelPlanner.Api.Models.Entities;

public class TripPhoto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Trip_Photo_Id { get; set; }
    [MaxLength(300)]
    public string Caption { get; set; }
    [Required, MaxLength(32)]
    public string Content_Type { get; set; }
    public byte[] Image_Data { get; set; }
    public DateTime Created_At { get; set; }

    [ForeignKey("Trip")]
    public int Trip_Id_Fk { get; set; }
    public Trip Trip { get; set; }

    [ForeignKey("User")]
    public int User_Id_Fk { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; }
}
