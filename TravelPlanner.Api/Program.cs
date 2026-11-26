using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Services;
using TravelPlanner.Api.UniversalMethod;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TravelPlanner.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 16 * 1024);
builder.Services.AddDbContext<ContextDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")), ServiceLifetime.Scoped);

// JWT
builder.Services.AddSingleton<JwtGenerator>();
builder.Services.AddHttpContextAccessor();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Задайте Jwt:Key длиной не меньше 32 байт.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/hubs/trip-chat"))
                    context.Token = context.Request.Query["access_token"];
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// HTTP
builder.Services.AddScoped<TripAccess>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITripsService, TripsService>();
builder.Services.AddScoped<IInvitationsService, InvitationsService>();
builder.Services.AddScoped<IProposalsService, ProposalsService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IExpensesService, ExpensesService>();
builder.Services.AddScoped<IBalancesService, BalancesService>();
builder.Services.AddScoped<IRepaymentsService, RepaymentsService>();
builder.Services.AddScoped<ICommentsService, CommentsService>();
builder.Services.AddScoped<IPlacesService, PlacesService>();
builder.Services.AddScoped<IPhotosService, PhotosService>();
builder.Services.AddScoped<IMessagesService, MessagesService>();
builder.Services.AddScoped<IExportsService, ExportsService>();

// Blazor
builder.Services.AddCors(options => options.AddPolicy("Blazor", policy =>
    policy.WithOrigins("http://localhost:5102", "https://localhost:7102")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Blazor");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TripChatHub>("/hubs/trip-chat", options => options.CloseOnAuthenticationExpiration = true);

app.Run();

public partial class Program { }
