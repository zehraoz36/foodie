using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YemekApp.API.Controllers;

[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected ulong CurrentUserId => ulong.Parse(User.FindFirst("sub")!.Value, System.Globalization.CultureInfo.InvariantCulture);
    protected bool IsOwner(ulong? userId) => userId is null || userId == CurrentUserId;
}
