using System;

public class Azure
{
	public Azure()
	{
        using Microsoft.AspNetCore.Mvc;
        using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly AzureBlobStorageService _storage;

    public UploadsController(AzureBlobStorageService storage)
    {
        _storage = storage;
    }

    [HttpPost("banner")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(
        IFormFile file)
    {
        try
        {
            var url = await _storage.UploadImageAsync(file);

            return Ok(new
            {
                success = true,
                imageUrl = url
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                error = "Image upload failed."
            });
        }
    }
}
	}
}
