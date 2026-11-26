using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TravelPlanner.Blazor;
using TravelPlanner.Blazor.Services;
using TravelPlanner.Blazor.ApiRequest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5101/"),
    Timeout = TimeSpan.FromSeconds(30)
});
builder.Services.AddScoped<ApiRequest>();
builder.Services.AddScoped<CurrentUser>();

await builder.Build().RunAsync();
