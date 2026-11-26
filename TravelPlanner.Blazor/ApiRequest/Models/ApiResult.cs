namespace TravelPlanner.Blazor.Models;

public class ApiResult<T> : ApiStatus
{
    public T? data { get; set; }
}
