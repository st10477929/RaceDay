using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController(IApiClient api) : AppController
{
    private bool IsLoggedIn => HttpContext.Session.GetInt32(SessionKeys.UserId) is not null;

    [HttpGet]
    public IActionResult Register() =>
        IsLoggedIn ? RedirectToAction("Index", "Home") : View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.PostAsync<UserDto>("api/auth/register", new
        {
            model.FullName, model.Email, model.Password, model.Role, model.PhoneNumber
        });

        if (!result.Ok)
        {
            ModelState.AddModelError("", result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Expired"] = expired;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        // Clear any old session first (prevents session fixation). ApiClient saves the new API cookie during the call below.
        HttpContext.Session.Clear();

        var result = await api.PostAsync<UserDto>("api/auth/login", new { model.Email, model.Password });
        if (!result.Ok || result.Data is null)
        {
            ModelState.AddModelError("", result.Error ?? "Login failed.");
            return View(model);
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        // Role-specific landing page.
        return result.Data.Role == RoleNames.Organiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await api.PostAsync<object>("api/auth/logout");   // ends the API session
        HttpContext.Session.Clear();                        // ends the MVC session (also drops the saved API cookie)
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
