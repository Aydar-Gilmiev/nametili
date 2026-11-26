using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TravelPlanner.Api.Models.Entities;

namespace TravelPlanner.Api.UniversalMethod;

public class JwtGenerator
{
    private readonly string _secretKey;

    public JwtGenerator(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Не задан ключ JWT.");
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.User_Id.ToString()),
            new Claim(ClaimTypes.Name, user.User_Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
