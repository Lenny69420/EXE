using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuatBook.Filters
{
    public class AdminOnlyFilter : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var roleId = context.HttpContext.Session.GetInt32("RoleId");

            if (!userId.HasValue)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang Login
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
            else if (roleId == 2)
            {
                // Nếu là User (roleId = 2) và đã đăng nhập, chuyển hướng về trang chủ ("/")
                context.Result = new RedirectResult("/");
            }
            else if (roleId != 1)
            {
                // Nếu không phải Admin (roleId != 1) và không phải User (roleId != 2), chuyển hướng đến trang Login
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Không cần xử lý sau khi action thực thi
        }
    }
}