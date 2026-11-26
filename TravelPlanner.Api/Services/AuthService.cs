using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Models.Entities;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.UniversalMethod;

namespace TravelPlanner.Api.Services;

public class AuthService : IAuthService
{
    private readonly ContextDb _db;
    private readonly JwtGenerator _jwtGenerator;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(ContextDb db, JwtGenerator jwtGenerator)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<IActionResult> RegisterAsync(RegisterRequest request)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var password = request.Password ?? "";
        if (string.IsNullOrWhiteSpace(request.Name) || !EmailHelper.IsValid(email) || password.Length < 8)
            return new BadRequestObjectResult("Укажите имя, почту и пароль не короче 8 символов.");

        if (await _db.Users.AnyAsync(x => x.Email == email))
            return new ConflictObjectResult("Пользователь с такой почтой уже есть.");

        var user = new User { User_Name = request.Name.Trim(), Email = email };
        user.Password_Hash = _passwordHasher.HashPassword(user, password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new OkObjectResult(new
        {
            token = _jwtGenerator.GenerateToken(user),
            user = new { Id = user.User_Id, Name = user.User_Name, user.Email }
        });
    }

    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.Password_Hash, request.Password ?? "") == PasswordVerificationResult.Failed)
            return new UnauthorizedObjectResult("Неверная почта или пароль.");

        return new OkObjectResult(new
        {
            token = _jwtGenerator.GenerateToken(user),
            user = new { Id = user.User_Id, Name = user.User_Name, user.Email }
        });
    }
}
