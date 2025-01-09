using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using EasyLogin.Datax;
using Microsoft.EntityFrameworkCore;

namespace EasyLogin.Services
{
    public class ValidateTokenAttribute : ActionFilterAttribute
    {
        private readonly ApplicationDbContext _db;

        public ValidateTokenAttribute(ApplicationDbContext db)
        {
            _db = db;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.AuthToken == token);
            if (user == null || user.AuthToken != token)
            {
                context.Result = new JsonResult(new ServerResult<string>(false, "Unauthorized", error: "Invalid token"));
                return;
            }

            await next();
        }
    }

}
