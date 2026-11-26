using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanner.Api.Models.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int User_Id { get; set; }

    public string User_Name { get; set; }
    public string Email { get; set; }

    public string Password_Hash { get; set; }
}
