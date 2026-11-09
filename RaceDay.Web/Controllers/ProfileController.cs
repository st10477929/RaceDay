using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Any logged-in user can view/update their personal information and upload a profile picture.
[SessionAuthorize]
public class ProfileController(IApiClient api) : AppController
{
    public async Task<IActionResult> Index()
    {
        var r = await api.GetAsync<UserDto>("api/users/me");
        if (!r.Ok || r.Data is null)
        {
            TempData["Error"] = r.Error ?? "Could not load your profile.";
            return RedirectToAction("Index", "Home");
        }
        return View(ToVm(r.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-load read-only fields so the page still renders fully.
            var current = await api.GetAsync<UserDto>("api/users/me");
            if (current.Data is not null) { model.Email = current.Data.Email; model.Role = current.Data.Role; model.ProfilePictureUrl = current.Data.ProfilePictureUrl; }
            return View("Index", model);
        }

        var r = await api.PutAsync<UserDto>("api/users/me", new { model.FullName, model.PhoneNumber });
        if (r.Ok && r.Data is not null)
        {
            HttpContext.Session.SetString(SessionKeys.FullName, r.Data.FullName);
            TempData["Success"] = "Profile updated.";
        }
        else TempData["Error"] = r.Error ?? "Could not update your profile.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        // MVC never talks to Azure: the file goes to the API, which stores it in Blob Storage.
        var r = await api.UploadAsync<UserDto>("api/users/me/picture", file);
        if (r.Ok) TempData["Success"] = "Profile picture updated.";
        else TempData["Error"] = r.Error ?? "Upload failed.";
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToVm(UserDto u) => new()
    {
        FullName = u.FullName, PhoneNumber = u.PhoneNumber, Email = u.Email, Role = u.Role, ProfilePictureUrl = u.ProfilePictureUrl
    };
}
