using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger)
    {
        _context = context;
        _logger = logger;
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
}

