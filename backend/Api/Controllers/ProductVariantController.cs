using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class ProductVariantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ProductVariantController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get all variants for a product
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetProductVariants(int productId)
    {
        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .Select(v => new ProductVariantResponse
            {
                Id = v.Id,
                ProductId = v.ProductId,
                Color = v.Color,
                ColorHex = v.ColorHex,
                Attributes = v.Attributes,
                ImageUrl = v.ImageUrl,
                MediaUrls = v.MediaUrls,
                PriceOverride = v.PriceOverride,
                SKU = v.SKU,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .OrderBy(v => v.Color)
            .ToListAsync();

        return Ok(variants);
    }

    /// <summary>
    /// Get a specific variant by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductVariantResponse>> GetVariant(int id)
    {
        var variant = await _context.ProductVariants
            .Where(v => v.Id == id)
            .Select(v => new ProductVariantResponse
            {
                Id = v.Id,
                ProductId = v.ProductId,
                Color = v.Color,
                ColorHex = v.ColorHex,
                Attributes = v.Attributes,
                ImageUrl = v.ImageUrl,
                MediaUrls = v.MediaUrls,
                PriceOverride = v.PriceOverride,
                SKU = v.SKU,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (variant == null)
        {
            return NotFound($"Variant with ID {id} not found.");
        }

        return Ok(variant);
    }

    /// <summary>
    /// Create a new variant for a product (Admin or SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductVariantResponse>> CreateVariant([FromBody] CreateProductVariantRequest request, [FromQuery] int productId)
    {
        // Color is optional now - can use Attributes instead
        // Validate that either Color or Attributes is provided
        if (string.IsNullOrWhiteSpace(request.Color) && string.IsNullOrWhiteSpace(request.Attributes))
        {
            return BadRequest("Either Color (legacy) or Attributes must be provided.");
        }

        // Verify product exists
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return BadRequest($"Product with ID {productId} not found.");
        }

        // Check if variant with same attributes already exists for this product
        // If using attributes, check by attributes; otherwise check by color (legacy)
        ProductVariant? existingVariant = null;
        
        if (!string.IsNullOrWhiteSpace(request.Attributes))
        {
            // Check by attributes
            existingVariant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.Attributes == request.Attributes);
            
            if (existingVariant != null)
            {
                return BadRequest($"A variant with the same attributes already exists for this product.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.Color))
        {
            // Legacy: Check by color
            existingVariant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.Color.ToLower() == request.Color.ToLower());
            
            if (existingVariant != null)
            {
                return BadRequest($"A variant with color '{request.Color}' already exists for this product.");
            }
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Color = request.Color?.Trim() ?? string.Empty, // Legacy
            ColorHex = request.ColorHex?.Trim(),
            Attributes = request.Attributes?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            MediaUrls = request.MediaUrls?.Trim(),
            PriceOverride = request.PriceOverride,
            SKU = request.SKU?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "ProductVariant",
            entityId: variant.Id.ToString(),
            action: "CREATE",
            actorUserId: GetCurrentUserId(),
            before: null,
            after: $"ProductId: {productId}, Color: {variant.Color}, SKU: {variant.SKU}"
        );

        var response = new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Color = variant.Color,
            ColorHex = variant.ColorHex,
            Attributes = variant.Attributes,
            ImageUrl = variant.ImageUrl,
            MediaUrls = variant.MediaUrls,
            PriceOverride = variant.PriceOverride,
            SKU = variant.SKU,
            IsActive = variant.IsActive,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };

        return CreatedAtAction(nameof(GetVariant), new { id = variant.Id }, response);
    }

    /// <summary>
    /// Update a variant (Admin or SuperAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductVariantResponse>> UpdateVariant(int id, [FromBody] UpdateProductVariantRequest request)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant == null)
        {
            return NotFound($"Variant with ID {id} not found.");
        }

        var before = $"Color: {variant.Color}, SKU: {variant.SKU}, PriceOverride: {variant.PriceOverride}";

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            // Check if new color already exists for this product (excluding current variant)
            var existingVariant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == variant.ProductId && 
                                         v.Color.ToLower() == request.Color.ToLower() && 
                                         v.Id != id);

            if (existingVariant != null)
            {
                return BadRequest($"A variant with color '{request.Color}' already exists for this product.");
            }

            variant.Color = request.Color.Trim();
        }

        if (request.ColorHex != null)
        {
            variant.ColorHex = request.ColorHex.Trim();
        }

        if (request.Attributes != null)
        {
            variant.Attributes = string.IsNullOrWhiteSpace(request.Attributes) ? null : request.Attributes.Trim();
        }

        if (request.ImageUrl != null)
        {
            variant.ImageUrl = request.ImageUrl.Trim();
        }

        if (request.MediaUrls != null)
        {
            variant.MediaUrls = string.IsNullOrWhiteSpace(request.MediaUrls) ? null : request.MediaUrls.Trim();
        }

        if (request.PriceOverride.HasValue)
        {
            variant.PriceOverride = request.PriceOverride.Value;
        }

        if (request.SKU != null)
        {
            variant.SKU = request.SKU.Trim();
        }

        if (request.IsActive.HasValue)
        {
            variant.IsActive = request.IsActive.Value;
        }

        variant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Color: {variant.Color}, SKU: {variant.SKU}, PriceOverride: {variant.PriceOverride}";
        await _auditService.LogAsync(
            entity: "ProductVariant",
            entityId: variant.Id.ToString(),
            action: "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var response = new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Color = variant.Color,
            ColorHex = variant.ColorHex,
            Attributes = variant.Attributes,
            ImageUrl = variant.ImageUrl,
            MediaUrls = variant.MediaUrls,
            PriceOverride = variant.PriceOverride,
            SKU = variant.SKU,
            IsActive = variant.IsActive,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a variant (Admin or SuperAdmin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant == null)
        {
            return NotFound($"Variant with ID {id} not found.");
        }

        var before = $"Color: {variant.Color}, SKU: {variant.SKU}";

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "ProductVariant",
            entityId: id.ToString(),
            action: "DELETE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: null
        );

        return NoContent();
    }
}

