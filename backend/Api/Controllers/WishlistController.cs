using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Api.Data;
using Api.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(ApplicationDbContext context, ILogger<WishlistController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all wishlist items for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WishlistItemResponse>>> GetWishlist()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                .Include(w => w.ProductVariant)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WishlistItemResponse
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name ?? "Unknown Product",
                    ProductSKU = w.Product.SKU,
                    ProductPrice = w.ProductVariant != null && w.ProductVariant.PriceOverride.HasValue
                        ? w.ProductVariant.PriceOverride.Value
                        : w.Product.Price,
                    ProductImageUrl = w.ProductVariant != null && !string.IsNullOrEmpty(w.ProductVariant.ImageUrl)
                        ? w.ProductVariant.ImageUrl
                        : w.Product.ImageUrl,
                    ProductVariantId = w.ProductVariantId,
                    VariantAttributes = w.ProductVariant != null ? w.ProductVariant.Attributes : null,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(wishlistItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist");
            return StatusCode(500, new { message = "An error occurred while retrieving wishlist" });
        }
    }

    /// <summary>
    /// Add an item to the wishlist
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WishlistItemResponse>> AddToWishlist([FromBody] CreateWishlistItemRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Verify product exists
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            // Verify variant exists if provided
            if (request.ProductVariantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId.Value && v.ProductId == request.ProductId);
                if (variant == null)
                {
                    return NotFound(new { message = "Product variant not found" });
                }
            }

            // Check if item already exists in wishlist
            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && 
                                         w.ProductId == request.ProductId && 
                                         w.ProductVariantId == request.ProductVariantId);

            if (existingItem != null)
            {
                return Conflict(new { message = "Item already exists in wishlist" });
            }

            // Create new wishlist item
            var wishlistItem = new Models.Wishlist
            {
                UserId = userId.Value,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            // Load related data for response
            await _context.Entry(wishlistItem)
                .Reference(w => w.Product)
                .LoadAsync();

            if (wishlistItem.ProductVariantId.HasValue)
            {
                await _context.Entry(wishlistItem)
                    .Reference(w => w.ProductVariant)
                    .LoadAsync();
            }

            var response = new WishlistItemResponse
            {
                Id = wishlistItem.Id,
                ProductId = wishlistItem.ProductId,
                ProductName = wishlistItem.Product.Name ?? "Unknown Product",
                ProductSKU = wishlistItem.Product.SKU,
                ProductPrice = wishlistItem.ProductVariant != null && wishlistItem.ProductVariant.PriceOverride.HasValue
                    ? wishlistItem.ProductVariant.PriceOverride.Value
                    : wishlistItem.Product.Price,
                ProductImageUrl = wishlistItem.ProductVariant != null && !string.IsNullOrEmpty(wishlistItem.ProductVariant.ImageUrl)
                    ? wishlistItem.ProductVariant.ImageUrl
                    : wishlistItem.Product.ImageUrl,
                ProductVariantId = wishlistItem.ProductVariantId,
                VariantAttributes = wishlistItem.ProductVariant != null ? wishlistItem.ProductVariant.Attributes : null,
                CreatedAt = wishlistItem.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to wishlist");
            return StatusCode(500, new { message = "An error occurred while adding item to wishlist" });
        }
    }

    /// <summary>
    /// Remove an item from the wishlist
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> RemoveFromWishlist(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (wishlistItem == null)
            {
                return NotFound(new { message = "Wishlist item not found" });
            }

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from wishlist" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from wishlist");
            return StatusCode(500, new { message = "An error occurred while removing item from wishlist" });
        }
    }

    /// <summary>
    /// Check if a product is in the user's wishlist
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<bool>> CheckWishlistItem([FromQuery] int productId, [FromQuery] int? productVariantId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && 
                             w.ProductId == productId && 
                             w.ProductVariantId == productVariantId);

            return Ok(new { isInWishlist = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist item");
            return StatusCode(500, new { message = "An error occurred while checking wishlist item" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}




