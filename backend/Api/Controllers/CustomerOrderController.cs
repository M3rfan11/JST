using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerOrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<CustomerOrderController> _logger;

    public CustomerOrderController(ApplicationDbContext context, IAuditService auditService, ILogger<CustomerOrderController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    private async Task<bool> IsCustomer()
    {
        var userId = GetCurrentUserId();
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
        return userRoles.Contains("Customer") || userRoles.Contains("StoreManager") || userRoles.Contains("SuperAdmin");
    }

    private string GenerateOrderNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"CUST{today:yyyyMMdd}";
        var count = _context.SalesOrders.Count(so => so.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    #region Shopping Cart Endpoints

    /// <summary>
    /// Get customer's shopping cart
    /// </summary>
    [HttpGet("cart")]
    public async Task<ActionResult<CartSummaryResponse>> GetCart()
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var cartItems = await _context.ShoppingCarts
                .Include(sc => sc.Product)
                .ThenInclude(p => p.Category)
                .Include(sc => sc.Product)
                .ThenInclude(p => p.ProductInventories.Where(pi => pi.WarehouseId == onlineWarehouse.Id))
                .Where(sc => sc.UserId == userId)
                .Select(sc => new CartItemResponse
                {
                    Id = sc.Id,
                    ProductId = sc.ProductId,
                    VariantId = sc.ProductVariantId,
                    ProductName = sc.Product.Name,
                    ProductSKU = sc.Product.SKU,
                    ProductDescription = sc.Product.Description,
                    Quantity = sc.Quantity,
                    UnitPrice = sc.UnitPrice,
                    TotalPrice = sc.Quantity * sc.UnitPrice,
                    Unit = sc.Unit,
                    CategoryName = sc.Product.Category.Name,
                    AvailableQuantity = sc.ProductVariantId.HasValue
                        ? _context.VariantInventories
                            .Where(vi => vi.ProductVariantId == sc.ProductVariantId.Value && vi.WarehouseId == onlineWarehouse.Id)
                            .Sum(vi => vi.Quantity)
                        : sc.Product.ProductInventories
                            .Where(pi => pi.WarehouseId == onlineWarehouse.Id)
                            .Sum(pi => pi.Quantity),
                    CreatedAt = sc.CreatedAt,
                    UpdatedAt = sc.UpdatedAt
                })
                .ToListAsync();

            var subTotal = cartItems.Sum(item => item.TotalPrice);
            var tax = subTotal * 0.1m; // 10% tax
            var total = subTotal + tax;

            var response = new CartSummaryResponse
            {
                Items = cartItems,
                SubTotal = subTotal,
                Tax = tax,
                Total = total,
                ItemCount = cartItems.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopping cart");
            return StatusCode(500, new { message = "An error occurred while retrieving the shopping cart" });
        }
    }

    /// <summary>
    /// Add item to shopping cart
    /// </summary>
    [HttpPost("cart/add")]
    public async Task<ActionResult<CartItemResponse>> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            // Check if product exists and is available
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductInventories.Where(pi => pi.WarehouseId == onlineWarehouse.Id))
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

            if (product == null)
            {
                return NotFound("Product not found or not available");
            }

            // Validate variant if provided
            if (request.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.Id == request.VariantId.Value && pv.ProductId == request.ProductId && pv.IsActive);
                
                if (variant == null)
                {
                    return BadRequest($"Variant ID {request.VariantId.Value} not found or not available for this product");
                }
            }

            // Get available quantity (always calculate for display, even if AlwaysAvailable)
            decimal availableQuantity = 0;
            if (request.VariantId.HasValue)
            {
                // Check variant inventory
                availableQuantity = await _context.VariantInventories
                    .Where(vi => vi.ProductVariantId == request.VariantId.Value && vi.WarehouseId == onlineWarehouse.Id)
                    .SumAsync(vi => vi.Quantity);
            }
            else
            {
                // Check product inventory
                availableQuantity = product.ProductInventories
                    .Where(pi => pi.WarehouseId == onlineWarehouse.Id)
                    .Sum(pi => pi.Quantity);
            }

            // Skip inventory check if product is marked as AlwaysAvailable
            if (!product.AlwaysAvailable)
            {
                if (availableQuantity < request.Quantity)
                {
                    return BadRequest($"Insufficient stock. Available: {availableQuantity}");
                }
            }

            // Check if item already exists in cart (same product AND variant)
            var existingCartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(sc => sc.UserId == userId 
                    && sc.ProductId == request.ProductId 
                    && sc.ProductVariantId == request.VariantId);

            if (existingCartItem != null)
            {
                // Update quantity
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new item
                // Use variant price if variant has price override, otherwise use product price
                decimal unitPrice = product.Price;
                if (request.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(request.VariantId.Value);
                    if (variant?.PriceOverride.HasValue == true)
                    {
                        unitPrice = variant.PriceOverride.Value;
                    }
                }

                var cartItem = new ShoppingCart
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    ProductVariantId = request.VariantId,
                    Quantity = request.Quantity,
                    UnitPrice = unitPrice,
                    Unit = product.Unit
                };
                _context.ShoppingCarts.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            // Return updated cart item
            var cartItemResponse = await _context.ShoppingCarts
                .Include(sc => sc.Product)
                .ThenInclude(p => p.Category)
                .Where(sc => sc.UserId == userId 
                    && sc.ProductId == request.ProductId 
                    && sc.ProductVariantId == request.VariantId)
                .Select(sc => new CartItemResponse
                {
                    Id = sc.Id,
                    ProductId = sc.ProductId,
                    VariantId = sc.ProductVariantId,
                    ProductName = sc.Product.Name,
                    ProductSKU = sc.Product.SKU,
                    ProductDescription = sc.Product.Description,
                    Quantity = sc.Quantity,
                    UnitPrice = sc.UnitPrice,
                    TotalPrice = sc.Quantity * sc.UnitPrice,
                    Unit = sc.Unit,
                    CategoryName = sc.Product.Category.Name,
                    AvailableQuantity = availableQuantity,
                    CreatedAt = sc.CreatedAt,
                    UpdatedAt = sc.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return Ok(cartItemResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return StatusCode(500, new { message = "An error occurred while adding item to cart" });
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("cart/{cartItemId}")]
    public async Task<ActionResult<CartItemResponse>> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var cartItem = await _context.ShoppingCarts
                .Include(sc => sc.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(sc => sc.Id == cartItemId && sc.UserId == userId);

            if (cartItem == null)
            {
                return NotFound("Cart item not found");
            }

            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            // Get the product to check AlwaysAvailable flag
            var product = await _context.Products.FindAsync(cartItem.ProductId);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Get available quantity (always calculate for display, even if AlwaysAvailable)
            var availableQuantity = await _context.ProductInventories
                .Where(pi => pi.ProductId == cartItem.ProductId && pi.WarehouseId == onlineWarehouse.Id)
                .SumAsync(pi => pi.Quantity);

            // Skip inventory check if product is marked as AlwaysAvailable
            if (!product.AlwaysAvailable)
            {
                if (availableQuantity < request.Quantity)
                {
                    return BadRequest($"Insufficient stock. Available: {availableQuantity}");
                }
            }

            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new CartItemResponse
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Name,
                ProductSKU = cartItem.Product.SKU,
                ProductDescription = cartItem.Product.Description,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.Quantity * cartItem.UnitPrice,
                Unit = cartItem.Unit,
                CategoryName = cartItem.Product.Category.Name,
                AvailableQuantity = availableQuantity,
                CreatedAt = cartItem.CreatedAt,
                UpdatedAt = cartItem.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return StatusCode(500, new { message = "An error occurred while updating cart item" });
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("cart/{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var cartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(sc => sc.Id == cartItemId && sc.UserId == userId);

            if (cartItem == null)
            {
                return NotFound("Cart item not found");
            }

            _context.ShoppingCarts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            return StatusCode(500, new { message = "An error occurred while removing item from cart" });
        }
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    [HttpDelete("cart/clear")]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var cartItems = await _context.ShoppingCarts
                .Where(sc => sc.UserId == userId)
                .ToListAsync();

            _context.ShoppingCarts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new { message = "An error occurred while clearing cart" });
        }
    }

    #endregion

    #region Customer Order Endpoints

    /// <summary>
    /// Create guest order (no authentication required)
    /// </summary>
    [HttpPost("guest-order")]
    [AllowAnonymous]
    public async Task<ActionResult<GuestOrderResponse>> CreateGuestOrder([FromBody] CreateGuestOrderRequest request)
    {
        try
        {
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store" || w.IsActive);
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest("No items provided");
            }

            // Validate inventory availability (skip check for AlwaysAvailable products)
            foreach (var item in request.Items)
            {
                // Get the product to check AlwaysAvailable flag
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product ID {item.ProductId} not found");
                }

                // Skip inventory check if product is marked as AlwaysAvailable
                if (product.AlwaysAvailable)
                {
                    continue;
                }

                // Check variant inventory if variantId is provided
                if (item.VariantId.HasValue)
                {
                    var variantInventory = await _context.VariantInventories
                        .Where(vi => vi.ProductVariantId == item.VariantId.Value && vi.WarehouseId == onlineWarehouse.Id)
                        .SumAsync(vi => vi.Quantity);

                    if (variantInventory < item.Quantity)
                    {
                        return BadRequest($"Insufficient stock for variant ID {item.VariantId.Value}. Available: {variantInventory}");
                    }
                }
                else
                {
                    // Check product inventory
                    var availableQuantity = await _context.ProductInventories
                        .Where(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id)
                        .SumAsync(pi => pi.Quantity);

                    if (availableQuantity < item.Quantity)
                    {
                        return BadRequest($"Insufficient stock for product ID {item.ProductId}. Available: {availableQuantity}");
                    }
                }
            }

            // Calculate subtotal
            var subtotal = request.Items.Sum(item => item.Quantity * item.UnitPrice);
            var shippingCost = request.ShippingCost ?? 0;
            var orderAmount = subtotal + shippingCost;
            
            // Check if user is authenticated (even though endpoint is AllowAnonymous)
            int? authenticatedUserId = null;
            try
            {
                var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userIdClaim != null)
                {
                    authenticatedUserId = int.Parse(userIdClaim.Value);
                }
            }
            catch
            {
                // User is not authenticated, continue as guest
            }

            // Apply promo code if provided
            decimal discountAmount = 0;
            PromoCode? appliedPromoCode = null;
            int? promoCodeUserId = authenticatedUserId; // Use authenticated user ID if available
            
            if (!string.IsNullOrEmpty(request.PromoCode))
            {
                // IMPORTANT: Promo codes are only available for signed-in users
                // Reject promo codes for guest users
                if (authenticatedUserId == null)
                {
                    // Guest user trying to use promo code - reject it silently (don't apply discount)
                    appliedPromoCode = null;
                }
                else
                {
                    appliedPromoCode = await _context.PromoCodes
                        .Include(pc => pc.PromoCodeUsers)
                        .Include(pc => pc.PromoCodeProducts)
                        .FirstOrDefaultAsync(pc => pc.Code.ToUpper() == request.PromoCode.ToUpper() && pc.IsActive);

                    if (appliedPromoCode != null)
                    {
                        var now = DateTime.UtcNow;
                        var orderProductIds = request.Items.Select(i => i.ProductId).ToList();
                        
                        // Check if expired
                        if (appliedPromoCode.EndDate.HasValue && appliedPromoCode.EndDate.Value < now)
                        {
                            appliedPromoCode = null; // Code expired
                        }
                        // Check usage limit
                        else if (appliedPromoCode.UsageLimit.HasValue && appliedPromoCode.UsedCount >= appliedPromoCode.UsageLimit.Value)
                        {
                            appliedPromoCode = null; // Usage limit reached
                        }
                        // Check minimum order amount
                        else if (appliedPromoCode.MinimumOrderAmount.HasValue && orderAmount < appliedPromoCode.MinimumOrderAmount.Value)
                        {
                            appliedPromoCode = null; // Minimum order amount not met
                        }
                        
                        // Check if assigned to specific users
                        if (appliedPromoCode != null && appliedPromoCode.PromoCodeUsers.Any())
                        {
                            if (!appliedPromoCode.PromoCodeUsers.Any(pcu => pcu.UserId == authenticatedUserId))
                            {
                                appliedPromoCode = null; // User not in allowed list
                            }
                        }
                        
                        // Check per-user usage limit (for all promocodes, not just user-specific ones)
                        if (appliedPromoCode != null && appliedPromoCode.UsageLimitPerUser.HasValue)
                        {
                            var userUsageCount = await _context.PromoCodeUsages
                                .CountAsync(pcu => pcu.PromoCodeId == appliedPromoCode.Id && pcu.UserId == authenticatedUserId);
                            
                            if (userUsageCount >= appliedPromoCode.UsageLimitPerUser.Value)
                            {
                                appliedPromoCode = null; // User has reached their limit
                            }
                        }
                        
                        // Check if applicable to specific products
                        if (appliedPromoCode != null && appliedPromoCode.PromoCodeProducts.Any())
                        {
                            if (!orderProductIds.Any(id => appliedPromoCode.PromoCodeProducts.Any(pcp => pcp.ProductId == id)))
                            {
                                appliedPromoCode = null; // No matching products
                            }
                        }

                        // Calculate discount if promo code is valid
                        if (appliedPromoCode != null)
                        {
                            if (appliedPromoCode.DiscountType == "Percentage")
                            {
                                discountAmount = orderAmount * (appliedPromoCode.DiscountValue / 100m);
                            }
                            else if (appliedPromoCode.DiscountType == "FixedAmount")
                            {
                                discountAmount = appliedPromoCode.DiscountValue;
                            }

                            // Apply maximum discount limit
                            if (appliedPromoCode.MaximumDiscountAmount.HasValue && discountAmount > appliedPromoCode.MaximumDiscountAmount.Value)
                            {
                                discountAmount = appliedPromoCode.MaximumDiscountAmount.Value;
                            }
                        }
                    }
                }
            }
            
            // Create order
            var orderNumber = GenerateGuestOrderNumber();
            var totalAmount = orderAmount - discountAmount;

            // For guest orders, we need to use a valid user ID due to foreign key constraint
            // Use the first admin user as the system user for guest orders
            var systemUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"));
            
            if (systemUser == null)
            {
                // Fallback to first user if no admin found
                systemUser = await _context.Users.FirstOrDefaultAsync();
                if (systemUser == null)
                {
                    return StatusCode(500, new { message = "System configuration error: No users found in database" });
                }
            }

            // All orders start with pending status
            string initialStatus = "pending";

            var order = new SalesOrder
            {
                OrderNumber = orderNumber,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                OrderDate = DateTime.UtcNow,
                DeliveryDate = request.DeliveryDate,
                TotalAmount = totalAmount,
                Status = initialStatus,
                PaymentStatus = "pending",
                PaymentMethod = request.PaymentMethod ?? "Cash on Delivery",
                Notes = request.Notes,
                CreatedByUserId = systemUser.Id, // Use system/admin user for guest orders
                CreatedAt = DateTime.UtcNow
            };

            _context.SalesOrders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items with variant tracking
            var salesItems = request.Items.Select(item => new SalesItem
            {
                SalesOrderId = order.Id,
                ProductId = item.ProductId,
                ProductVariantId = item.VariantId, // Track which variant was ordered
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                Unit = item.Unit ?? "piece",
                WarehouseId = onlineWarehouse.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.SalesItems.AddRange(salesItems);

            // Deduct inventory for each item
            foreach (var item in request.Items)
            {
                // Get the product to check AlwaysAvailable flag
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null && product.AlwaysAvailable)
                {
                    continue; // Skip inventory deduction for AlwaysAvailable products
                }

                if (item.VariantId.HasValue)
                {
                    // Deduct from variant inventory
                    var variantInventory = await _context.VariantInventories
                        .FirstOrDefaultAsync(vi => vi.ProductVariantId == item.VariantId.Value && vi.WarehouseId == onlineWarehouse.Id);
                    
                    if (variantInventory != null)
                    {
                        variantInventory.Quantity -= item.Quantity;
                        variantInventory.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Deducted {Quantity} from variant inventory {VariantId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.VariantId.Value, variantInventory.Quantity);
                    }
                }
                else
                {
                    // Deduct from product inventory
                    var productInventory = await _context.ProductInventories
                        .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id);
                    
                    if (productInventory != null)
                    {
                        productInventory.Quantity -= item.Quantity;
                        productInventory.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Deducted {Quantity} from product inventory {ProductId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.ProductId, productInventory.Quantity);
                    }
                }
            }

            // Create initial tracking entry
            var tracking = new OrderTracking
            {
                OrderId = order.Id,
                Status = "pending",
                Notes = "Order created",
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = systemUser.Id // Use system user for guest orders
            };
            _context.OrderTrackings.Add(tracking);

            // Track promo code usage if applied
            // Record promo code usage
            if (appliedPromoCode != null && discountAmount > 0)
            {
                appliedPromoCode.UsedCount++;
                
                var promoCodeUsage = new PromoCodeUsage
                {
                    PromoCodeId = appliedPromoCode.Id,
                    SalesOrderId = order.Id,
                    UserId = promoCodeUserId, // null for guest orders
                    DiscountAmount = discountAmount,
                    UsedAt = DateTime.UtcNow
                };
                
                _context.PromoCodeUsages.Add(promoCodeUsage);
            }

            await _context.SaveChangesAsync();

            // Return guest order response
            return Ok(new GuestOrderResponse
            {
                OrderId = order.Id,
                OrderNumber = orderNumber,
                CustomerEmail = order.CustomerEmail,
                TotalAmount = totalAmount,
                Status = order.Status,
                Message = "Order placed successfully. You can track your order by logging in with your email."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    private string GenerateGuestOrderNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"GUEST{today:yyyyMMdd}";
        var count = _context.SalesOrders.Count(so => so.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    /// <summary>
    /// Track order by order number and email (for guest orders)
    /// </summary>
    [HttpPost("track-order")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerOrderResponse>> TrackOrder([FromBody] TrackOrderRequest request)
    {
        try
        {
            // Normalize order number (remove # if present, trim whitespace, uppercase)
            var normalizedOrderNumber = request.OrderNumber.Trim().ToUpper().Replace("#", "");
            
            // Normalize email (trim and lowercase)
            var normalizedEmail = request.Email.Trim().ToLower();
            
            // Get all orders first, then filter in memory for case-insensitive matching
            var orders = await _context.SalesOrders
                .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
                .Where(so => so.CustomerEmail != null)
                .ToListAsync();
            
            var order = orders.FirstOrDefault(so => 
                so.OrderNumber.Trim().ToUpper().Replace("#", "") == normalizedOrderNumber && 
                so.CustomerEmail!.Trim().ToLower() == normalizedEmail);

            if (order == null)
            {
                // Log available orders for debugging (first 5 matching the order number pattern)
                var matchingOrders = orders
                    .Where(so => so.OrderNumber.Trim().ToUpper().Replace("#", "").StartsWith(normalizedOrderNumber.Substring(0, Math.Min(8, normalizedOrderNumber.Length))))
                    .Take(5)
                    .Select(so => new { so.OrderNumber, so.CustomerEmail })
                    .ToList();
                
                _logger.LogWarning("Order not found for OrderNumber: {OrderNumber}, Email: {Email}. Found {Count} similar orders: {Orders}", 
                    normalizedOrderNumber, normalizedEmail, matchingOrders.Count, 
                    string.Join(", ", matchingOrders.Select(o => $"{o.OrderNumber}/{o.CustomerEmail}")));
                
                return NotFound("Order not found. Please check your order number and email.");
            }

            var response = await GetOrderResponse(order.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order");
            return StatusCode(500, new { message = "An error occurred while tracking the order" });
        }
    }

    /// <summary>
    /// Create order from cart or direct items (requires authentication)
    /// </summary>
    [HttpPost("order")]
    [Authorize]
    public async Task<ActionResult<CustomerOrderResponse>> CreateOrder([FromBody] CreateCustomerOrderRequest request)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            List<CreateOnlineOrderItemRequest> orderItems;

            if (request.UseCartItems)
            {
                // Get items from cart
                var cartItems = await _context.ShoppingCarts
                    .Include(sc => sc.Product)
                    .Where(sc => sc.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return BadRequest("Cart is empty");
                }

                orderItems = cartItems.Select(sc => new CreateOnlineOrderItemRequest
                {
                    ProductId = sc.ProductId,
                    VariantId = sc.ProductVariantId,
                    Quantity = sc.Quantity,
                    UnitPrice = sc.UnitPrice,
                    Unit = sc.Unit
                }).ToList();
            }
            else
            {
                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest("No items provided");
                }
                orderItems = request.Items;
            }

            // Validate inventory availability (skip check for AlwaysAvailable products)
            foreach (var item in orderItems)
            {
                // Get the product to check AlwaysAvailable flag
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product ID {item.ProductId} not found");
                }

                // Skip inventory check if product is marked as AlwaysAvailable
                if (product.AlwaysAvailable)
                {
                    continue;
                }

                // Check variant inventory if variantId is provided
                decimal availableQuantity = 0;
                if (item.VariantId.HasValue)
                {
                    availableQuantity = await _context.VariantInventories
                        .Where(vi => vi.ProductVariantId == item.VariantId.Value && vi.WarehouseId == onlineWarehouse.Id)
                        .SumAsync(vi => vi.Quantity);
                }
                else
                {
                    availableQuantity = await _context.ProductInventories
                        .Where(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id)
                        .SumAsync(pi => pi.Quantity);
                }

                if (availableQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for product ID {item.ProductId}. Available: {availableQuantity}");
                }
            }

            // Calculate subtotal and apply promo code if provided
            var subtotal = orderItems.Sum(item => item.Quantity * item.UnitPrice);
            var shippingCost = 0m; // Can be added to request if needed
            var orderAmount = subtotal + shippingCost;
            
            decimal discountAmount = 0;
            PromoCode? appliedPromoCode = null;
            
            if (!string.IsNullOrEmpty(request.PromoCode))
            {
                appliedPromoCode = await _context.PromoCodes
                    .Include(pc => pc.PromoCodeUsers)
                    .Include(pc => pc.PromoCodeProducts)
                    .FirstOrDefaultAsync(pc => pc.Code.ToUpper() == request.PromoCode.ToUpper() && pc.IsActive);

                if (appliedPromoCode != null)
                {
                    var now = DateTime.UtcNow;
                    var orderProductIds = orderItems.Select(i => i.ProductId).ToList();
                    
                    // Check if expired
                    if (appliedPromoCode.EndDate.HasValue && appliedPromoCode.EndDate.Value < now)
                    {
                        appliedPromoCode = null; // Code expired
                    }
                    // Check usage limit
                    else if (appliedPromoCode.UsageLimit.HasValue && appliedPromoCode.UsedCount >= appliedPromoCode.UsageLimit.Value)
                    {
                        appliedPromoCode = null; // Usage limit reached
                    }
                    // Check minimum order amount
                    else if (appliedPromoCode.MinimumOrderAmount.HasValue && orderAmount < appliedPromoCode.MinimumOrderAmount.Value)
                    {
                        appliedPromoCode = null; // Minimum order amount not met
                    }
                    // Check if assigned to specific users
                    if (appliedPromoCode.PromoCodeUsers.Any())
                    {
                        if (!appliedPromoCode.PromoCodeUsers.Any(pcu => pcu.UserId == userId))
                        {
                            appliedPromoCode = null; // User not in allowed list
                        }
                    }
                    
                    // Check per-user usage limit (for all promocodes, not just user-specific ones)
                    if (appliedPromoCode != null && appliedPromoCode.UsageLimitPerUser.HasValue)
                    {
                        var userUsageCount = await _context.PromoCodeUsages
                            .CountAsync(pcu => pcu.PromoCodeId == appliedPromoCode.Id && pcu.UserId == userId);
                        
                        if (userUsageCount >= appliedPromoCode.UsageLimitPerUser.Value)
                        {
                            appliedPromoCode = null; // User has reached their limit
                        }
                    }
                    
                    // Check if applicable to specific products
                    if (appliedPromoCode != null && appliedPromoCode.PromoCodeProducts.Any())
                    {
                        if (!orderProductIds.Any(id => appliedPromoCode.PromoCodeProducts.Any(pcp => pcp.ProductId == id)))
                        {
                            appliedPromoCode = null; // No matching products
                        }
                    }

                    // Calculate discount if promo code is valid
                    if (appliedPromoCode != null)
                    {
                        if (appliedPromoCode.DiscountType == "Percentage")
                        {
                            discountAmount = orderAmount * (appliedPromoCode.DiscountValue / 100m);
                        }
                        else if (appliedPromoCode.DiscountType == "FixedAmount")
                        {
                            discountAmount = appliedPromoCode.DiscountValue;
                        }

                        // Apply maximum discount limit
                        if (appliedPromoCode.MaximumDiscountAmount.HasValue && discountAmount > appliedPromoCode.MaximumDiscountAmount.Value)
                        {
                            discountAmount = appliedPromoCode.MaximumDiscountAmount.Value;
                        }
                    }
                }
            }

            // Create order
            var orderNumber = GenerateOrderNumber();
            var totalAmount = orderAmount - discountAmount;

            // All orders start with pending status
            string initialStatus = "pending";

            var order = new SalesOrder
            {
                OrderNumber = orderNumber,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                OrderDate = DateTime.UtcNow,
                DeliveryDate = request.DeliveryDate,
                TotalAmount = totalAmount,
                Status = initialStatus,
                PaymentStatus = "pending",
                PaymentMethod = request.PaymentMethod ?? "Cash on Delivery",
                Notes = request.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SalesOrders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items with variant tracking
            var salesItems = orderItems.Select(item => new SalesItem
            {
                SalesOrderId = order.Id,
                ProductId = item.ProductId,
                ProductVariantId = item.VariantId, // Track which variant was ordered
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                Unit = item.Unit,
                WarehouseId = onlineWarehouse.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.SalesItems.AddRange(salesItems);

            // Deduct inventory for each item
            foreach (var item in orderItems)
            {
                // Get the product to check AlwaysAvailable flag
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null && product.AlwaysAvailable)
                {
                    continue; // Skip inventory deduction for AlwaysAvailable products
                }

                if (item.VariantId.HasValue)
                {
                    // Deduct from variant inventory
                    var variantInventory = await _context.VariantInventories
                        .FirstOrDefaultAsync(vi => vi.ProductVariantId == item.VariantId.Value && vi.WarehouseId == onlineWarehouse.Id);
                    
                    if (variantInventory != null)
                    {
                        variantInventory.Quantity -= item.Quantity;
                        variantInventory.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Deducted {Quantity} from variant inventory {VariantId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.VariantId.Value, variantInventory.Quantity);
                    }
                }
                else
                {
                    // Deduct from product inventory
                    var productInventory = await _context.ProductInventories
                        .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id);
                    
                    if (productInventory != null)
                    {
                        productInventory.Quantity -= item.Quantity;
                        productInventory.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Deducted {Quantity} from product inventory {ProductId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.ProductId, productInventory.Quantity);
                    }
                }
            }

            // Create initial tracking entry
            var tracking = new OrderTracking
            {
                OrderId = order.Id,
                Status = "pending",
                Notes = "Order created",
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.OrderTrackings.Add(tracking);

            // Track promo code usage if applied
            if (appliedPromoCode != null && discountAmount > 0)
            {
                appliedPromoCode.UsedCount++;
                
                var promoCodeUsage = new PromoCodeUsage
                {
                    PromoCodeId = appliedPromoCode.Id,
                    SalesOrderId = order.Id,
                    UserId = userId,
                    DiscountAmount = discountAmount,
                    UsedAt = DateTime.UtcNow
                };
                _context.PromoCodeUsages.Add(promoCodeUsage);
            }

            // Clear cart if order was created from cart
            if (request.UseCartItems)
            {
                var cartItems = await _context.ShoppingCarts
                    .Where(sc => sc.UserId == userId)
                    .ToListAsync();
                _context.ShoppingCarts.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync("CustomerOrder", order.Id.ToString(), "CREATE", 
                $"Customer order created: {orderNumber}", JsonSerializer.Serialize(new { OrderId = order.Id, OrderNumber = orderNumber }), userId);

            // Return order response
            var response = await GetOrderResponse(order.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Get customer's orders (both authenticated orders and guest orders by email)
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<CustomerOrderResponse>>> GetCustomerOrders()
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            
            // Get current user's email to match guest orders
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Get orders where:
            // 1. CreatedByUserId matches the logged-in user (authenticated orders)
            // 2. OR CustomerEmail matches the user's email (guest orders placed with same email)
            var orders = await _context.SalesOrders
                .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
                .Where(so => 
                    so.CreatedByUserId == userId || 
                    (!string.IsNullOrEmpty(currentUser.Email) && 
                     !string.IsNullOrEmpty(so.CustomerEmail) && 
                     so.CustomerEmail.ToLower() == currentUser.Email.ToLower()))
                .OrderByDescending(so => so.OrderDate)
                .ToListAsync();

            var responses = new List<CustomerOrderResponse>();
            foreach (var order in orders)
            {
                var response = await GetOrderResponse(order.Id);
                responses.Add(response);
            }

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer orders");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get specific order by ID (authenticated orders or guest orders by email)
    /// </summary>
    [HttpGet("order/{id}")]
    public async Task<ActionResult<CustomerOrderResponse>> GetOrder(int id)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            
            // Get current user's email to match guest orders
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(so => 
                    so.Id == id && 
                    (so.CreatedByUserId == userId || 
                     (!string.IsNullOrEmpty(currentUser.Email) && 
                      !string.IsNullOrEmpty(so.CustomerEmail) && 
                      so.CustomerEmail.ToLower() == currentUser.Email.ToLower())));

            if (order == null)
            {
                return NotFound("Order not found");
            }

            var response = await GetOrderResponse(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Track order by order number (for authenticated users - uses their email automatically)
    /// </summary>
    [HttpGet("track/{orderNumber}")]
    [Authorize]
    public async Task<ActionResult<CustomerOrderResponse>> TrackOrderByNumber(string orderNumber)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }

            var userId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Normalize order number (remove # if present, trim whitespace, uppercase)
            var normalizedOrderNumber = orderNumber.Trim().ToUpper().Replace("#", "");
            
            // Get all orders first, then filter in memory for case-insensitive matching
            var orders = await _context.SalesOrders
                .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
                .Where(so => 
                    so.OrderNumber.Trim().ToUpper().Replace("#", "") == normalizedOrderNumber &&
                    (so.CreatedByUserId == userId || 
                     (!string.IsNullOrEmpty(currentUser.Email) && 
                      !string.IsNullOrEmpty(so.CustomerEmail) && 
                      so.CustomerEmail.ToLower() == currentUser.Email.ToLower())))
                .ToListAsync();
            
            var order = orders.FirstOrDefault();

            if (order == null)
            {
                return NotFound("Order not found. Please check your order number.");
            }

            var response = await GetOrderResponse(order.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order by number");
            return StatusCode(500, new { message = "An error occurred while tracking the order" });
        }
    }

    #endregion

    #region Product Catalog for Customers

    /// <summary>
    /// Get available products for customers
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<CustomerProductResponse>>> GetProducts([FromQuery] int? categoryId = null, [FromQuery] string? search = null)
    {
        try
        {
            if (!await IsCustomer())
            {
                return StatusCode(403, new { message = "Access denied. Customer role required." });
            }
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductInventories.Where(pi => pi.WarehouseId == onlineWarehouse.Id))
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            var products = await query
                .Select(p => new CustomerProductResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    SKU = p.SKU,
                    Brand = p.Brand,
                    Unit = p.Unit,
                    CategoryName = p.Category.Name,
                    AvailableQuantity = p.ProductInventories
                        .Where(pi => pi.WarehouseId == onlineWarehouse.Id)
                        .Sum(pi => pi.Quantity),
                    IsAvailable = p.ProductInventories
                        .Where(pi => pi.WarehouseId == onlineWarehouse.Id)
                        .Sum(pi => pi.Quantity) > 0,
                    ImageUrl = p.ImageUrl
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new { message = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Get product categories for customers
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CustomerCategoryResponse>>> GetCategories()
    {
        try
        {
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new CustomerCategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products
                        .Where(p => p.IsActive && p.ProductInventories
                            .Any(pi => pi.WarehouseId == onlineWarehouse.Id && pi.Quantity > 0))
                        .Count()
                })
                .Where(c => c.ProductCount > 0)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { message = "An error occurred while retrieving categories" });
        }
    }

    #endregion

    #region Helper Methods

    private async Task<CustomerOrderResponse> GetOrderResponse(int orderId)
    {
        var order = await _context.SalesOrders
            .Include(so => so.SalesItems)
            .ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(so => so.Id == orderId);

        var trackingHistory = await _context.OrderTrackings
            .Include(ot => ot.UpdatedByUser)
            .Where(ot => ot.OrderId == orderId)
            .OrderBy(ot => ot.Timestamp)
            .Select(ot => new OrderTrackingResponse
            {
                Id = ot.Id,
                Status = ot.Status,
                Notes = ot.Notes,
                Location = ot.Location,
                Timestamp = ot.Timestamp,
                UpdatedByUserName = ot.UpdatedByUser != null ? ot.UpdatedByUser.FullName : "System"
            })
            .ToListAsync();

        var items = order.SalesItems.Select(si => new OnlineOrderItemResponse
        {
            Id = si.Id,
            ProductId = si.ProductId,
            ProductName = si.Product.Name,
            ProductSKU = si.Product.SKU,
            Quantity = si.Quantity,
            UnitPrice = si.UnitPrice,
            TotalPrice = si.TotalPrice,
            Unit = si.Unit
        }).ToList();

        return new CustomerOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName ?? string.Empty,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerAddress = order.CustomerAddress ?? string.Empty,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Notes = order.Notes,
            Items = items,
            TrackingHistory = trackingHistory
        };
    }

    #endregion
}
