using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SupportCopilot.Web.Security;

namespace SupportCopilot.Web.Controllers;

/// <summary>Demo sign-in: alice (Agent), sam (Supervisor), ayesha (Customer).</summary>
public sealed class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(model: returnUrl);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string? returnUrl)
    {
        var user = DemoUsers.All.FirstOrDefault(u => u.UserName == userName);
        if (user is null) return View(model: returnUrl);

        await HttpContext.SignInAsync(DemoUsers.ToPrincipal(user));
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
