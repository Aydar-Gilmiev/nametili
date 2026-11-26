using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.DatabaseContext;

namespace TravelPlanner.Api.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizedAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _roles;

        public RoleAuthorizedAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true
                || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                || userId <= 0)
            {
                context.Result = new JsonResult(new { error = "Пользователь не авторизован" }) { StatusCode = 401 };
                return;
            }

            var contextDb = context.HttpContext.RequestServices.GetRequiredService<ContextDb>();
            if (!await contextDb.Users.AnyAsync(value => value.User_Id == userId, context.HttpContext.RequestAborted))
            {
                context.Result = new JsonResult(new { error = "Пользователь не найден" }) { StatusCode = 401 };
                return;
            }

            if (_roles.Length > 0)
            {
                if (!context.RouteData.Values.TryGetValue("tripId", out var routeValue)
                    || !int.TryParse(routeValue?.ToString(), out var tripId) || tripId <= 0)
                {
                    context.Result = new JsonResult(new { error = "Для проверки роли нужна поездка" }) { StatusCode = 403 };
                    return;
                }

                var member = await contextDb.TripMembers.FirstOrDefaultAsync(value =>
                    value.Trip_Id_Fk == tripId && value.User_Id_Fk == userId, context.HttpContext.RequestAborted);

                if (member == null || !_roles.Contains(member.Role))
                {
                    context.Result = new JsonResult(new { error = "Нет прав для этой поездки" }) { StatusCode = 403 };
                    return;
                }
            }

            await next();
        }
    }
}
