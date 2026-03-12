using System.Security.Claims;
using CORE_BE.Data;
using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CORE_BE.Filters
{
    public class PermissionAttribute : TypeFilterAttribute
    {
        public PermissionAttribute(string menuUrl, string action)
            : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { menuUrl, action };
        }
    }

    public class PermissionFilter : IAsyncActionFilter
    {
        private readonly string _menuUrl;
        private readonly string _action;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionFilter(string menuUrl, string action, IUnitOfWork uow, UserManager<ApplicationUser> userManager)
        {
            _menuUrl = menuUrl;
            _action = action?.ToLower();
            _uow = uow;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any())
            {
                context.Result = new ObjectResult(new { message = "Tài khoản chưa được phân vai trò." }) { StatusCode = 403 };
                return;
            }

            var allRoleIds = _uow.GetRepository<ApplicationRole>()
                .GetAll(x => !x.IsDeleted && roles.Contains(x.Name))
                .Select(x => x.Id)
                .ToList();

            var menu = _uow.GetRepository<Menu>()
                .GetAll(x => !x.IsDeleted && x.Url == _menuUrl)
                .FirstOrDefault();

            if (menu == null)
            {
                context.Result = new ObjectResult(new { message = $"Không tìm thấy menu {_menuUrl} để kiểm tra quyền." }) { StatusCode = 403 };
                return;
            }

            var menuRoles = _uow.GetRepository<Menu_Role>()
                .GetAll(x => x.Menu_Id == menu.Id && allRoleIds.Contains(x.Role_Id) && !x.IsDeleted)
                .ToList();

            bool hasPermission = false;

            switch (_action)
            {
                case "view":
                    hasPermission = menuRoles.Any(x => x.View);
                    break;
                case "add":
                    hasPermission = menuRoles.Any(x => x.Add);
                    break;
                case "edit":
                    hasPermission = menuRoles.Any(x => x.Edit);
                    break;
                case "del":
                    hasPermission = menuRoles.Any(x => x.Del);
                    break;
                case "cof":
                    hasPermission = menuRoles.Any(x => x.Cof);
                    break;
                case "print":
                    hasPermission = menuRoles.Any(x => x.Print);
                    break;
            }

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new { message = $"Bạn không có quyền {_action} trên menu {_menuUrl}." })
                {
                    StatusCode = 403
                };
                return;
            }

            await next();
        }
    }
}
