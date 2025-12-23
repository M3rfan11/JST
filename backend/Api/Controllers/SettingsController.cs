using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    private readonly IWebHostEnvironment _environment;

    public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get shipping ticker messages (Public endpoint)
    /// </summary>
    [HttpGet("shipping-ticker")]
    [AllowAnonymous]
    public async Task<ActionResult<ShippingTickerMessagesResponse>> GetShippingTickerMessages()
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "ShippingTickerMessages");

            if (setting == null || string.IsNullOrEmpty(setting.Value))
            {
                // Return default messages if not set
                return Ok(new ShippingTickerMessagesResponse
                {
                    Messages = new List<string>
                    {
                        "FREE SHIPPING ON ORDERS OVER 1000 EGP",
                        "30-DAY RETURNS",
                        "AUTHENTIC EGYPTIAN CRAFTSMANSHIP"
                    }
                });
            }

            var messages = JsonSerializer.Deserialize<List<string>>(setting.Value) ?? new List<string>();
            return Ok(new ShippingTickerMessagesResponse { Messages = messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipping ticker messages");
            return StatusCode(500, new { message = "An error occurred while retrieving shipping ticker messages" });
        }
    }

    /// <summary>
    /// Update shipping ticker messages (Admin only)
    /// </summary>
    [HttpPut("shipping-ticker")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ShippingTickerMessagesResponse>> UpdateShippingTickerMessages([FromBody] ShippingTickerMessagesRequest request)
    {
        try
        {
            if (request.Messages == null || request.Messages.Count == 0)
            {
                return BadRequest(new { message = "At least one message is required" });
            }

            // Validate messages (max length, etc.)
            foreach (var message in request.Messages)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return BadRequest(new { message = "Messages cannot be empty" });
                }
                if (message.Length > 200)
                {
                    return BadRequest(new { message = "Each message must be 200 characters or less" });
                }
            }

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "ShippingTickerMessages");

            var messagesJson = JsonSerializer.Serialize(request.Messages);

            if (setting == null)
            {
                setting = new Models.Settings
                {
                    Key = "ShippingTickerMessages",
                    Value = messagesJson,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = messagesJson;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new ShippingTickerMessagesResponse { Messages = request.Messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping ticker messages");
            return StatusCode(500, new { message = "An error occurred while updating shipping ticker messages" });
        }
    }

    /// <summary>
    /// Get InstaPay QR code URL (Public endpoint)
    /// </summary>
    [HttpGet("instapay-qr")]
    [AllowAnonymous]
    public async Task<ActionResult<InstaPayQrResponse>> GetInstaPayQrCode()
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "InstaPayQrCode");

            if (setting == null || string.IsNullOrEmpty(setting.Value))
            {
                return Ok(new InstaPayQrResponse { QrCodeUrl = null });
            }

            return Ok(new InstaPayQrResponse { QrCodeUrl = setting.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving InstaPay QR code");
            return StatusCode(500, new { message = "An error occurred while retrieving InstaPay QR code" });
        }
    }

    /// <summary>
    /// Upload InstaPay QR code (Admin only)
    /// </summary>
    [HttpPost("instapay-qr")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<InstaPayQrResponse>> UploadInstaPayQrCode(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Validate file type (only images)
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Invalid file type. Only image files (JPG, PNG, GIF, WEBP) are allowed." });
            }

            // Validate file size (max 2MB for QR codes)
            if (file.Length > 2 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 2MB limit" });
            }

            // Save file to wwwroot/uploads/instapay-qr
            var wwwrootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(wwwrootPath, "uploads", "instapay-qr");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Delete old QR code if exists
            var oldSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "InstaPayQrCode");
            
            if (oldSetting != null && !string.IsNullOrEmpty(oldSetting.Value))
            {
                var oldFilePath = Path.Combine(wwwrootPath, oldSetting.Value.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old QR code file: {FilePath}", oldFilePath);
                    }
                }
            }

            // Save new file
            var fileName = $"instapay-qr-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var fileUrl = $"/uploads/instapay-qr/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save to database
            if (oldSetting == null)
            {
                oldSetting = new Models.Settings
                {
                    Key = "InstaPayQrCode",
                    Value = fileUrl,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Settings.Add(oldSetting);
            }
            else
            {
                oldSetting.Value = fileUrl;
                oldSetting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new InstaPayQrResponse { QrCodeUrl = fileUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading InstaPay QR code");
            return StatusCode(500, new { message = "An error occurred while uploading InstaPay QR code" });
        }
    }

    /// <summary>
    /// Delete InstaPay QR code (Admin only)
    /// </summary>
    [HttpDelete("instapay-qr")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> DeleteInstaPayQrCode()
    {
        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == "InstaPayQrCode");

            if (setting != null && !string.IsNullOrEmpty(setting.Value))
            {
                // Delete file
                var wwwrootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(wwwrootPath, setting.Value.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete QR code file: {FilePath}", filePath);
                    }
                }

                // Remove from database
                _context.Settings.Remove(setting);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "QR code deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting InstaPay QR code");
            return StatusCode(500, new { message = "An error occurred while deleting InstaPay QR code" });
        }
    }
}

