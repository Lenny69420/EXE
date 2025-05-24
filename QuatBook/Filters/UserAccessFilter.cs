using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuatBook.Filters
{
    public class UserAccessFilter : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                // Nếu action trả về JSON (như OrderDetails), trả về JSON error
                if (context.ActionDescriptor.EndpointMetadata.Any(em => em is HttpGetAttribute))
                {
                    context.Result = new JsonResult(new { success = false, message = "Please login to continue." });
                }
                else
                {
                    context.Result = new RedirectToActionResult("Login", "Account", null);
                }
            }
            else
            {
                // Lưu userId vào ViewData để sử dụng trong action
                context.HttpContext.Items["UserId"] = userId.Value;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Không cần xử lý sau khi action thực thi
        }
    }
}