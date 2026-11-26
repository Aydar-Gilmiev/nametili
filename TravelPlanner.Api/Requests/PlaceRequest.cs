using System.ComponentModel.DataAnnotations;

namespace TravelPlanner.Api.Requests;

public class PlaceRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";
    [StringLength(1000)]
    public string Note { get; set; } = "";
    [Range(-90, 90)]
    public double Latitude { get; set; }
    [Range(-180, 180)]
    public double Longitude { get; set; }
}
