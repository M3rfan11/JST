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
public class ProductAssemblyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ProductAssemblyController(ApplicationDbContext context, IAuditService auditService)
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
    /// Get all product assemblies
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<ProductAssemblyListResponse>>> GetProductAssemblies()
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        IQueryable<ProductAssembly> assembliesQuery = _context.ProductAssemblies
            .Include(pa => pa.CreatedByUser)
            .Include(pa => pa.CompletedByUser)
            .Include(pa => pa.Store)
            .Include(pa => pa.BillOfMaterials);

        // Apply store-scoped filtering
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // Only SuperAdmin can access - no store filtering needed
        if (!userRoles.Contains("SuperAdmin"))
        {
            return StatusCode(403, new { Message = "You don't have permission to view product assemblies" });
        }

        var assemblies = await assembliesQuery
            .OrderByDescending(pa => pa.CreatedAt)
            .Select(pa => new ProductAssemblyListResponse
            {
                Id = pa.Id,
                Name = pa.Name,
                Description = pa.Description,
                Quantity = pa.Quantity,
                Unit = pa.Unit,
                Status = pa.Status,
                CreatedByUserName = pa.CreatedByUser.FullName,
                CompletedByUserName = pa.CompletedByUser != null ? pa.CompletedByUser.FullName : null,
                CreatedAt = pa.CreatedAt,
                CompletedAt = pa.CompletedAt,
                StoreId = pa.StoreId,
                StoreName = pa.Store != null ? pa.Store.Name : null,
                IsActive = pa.IsActive,
                SalePrice = pa.SalePrice,
                MaterialCount = pa.BillOfMaterials.Count,
                CanStart = pa.Status == "Pending" && pa.BillOfMaterials.All(bom => bom.AvailableQuantity >= bom.RequiredQuantity)
            })
            .ToListAsync();

        return Ok(assemblies);
    }

    /// <summary>
    /// Get store inventory for assembly creation (simplified for store managers)
    /// </summary>
    [HttpGet("my-store-inventory")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<object>>> GetStoreInventoryForAssembly()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(currentUserId);
            
            // Store filtering removed - SuperAdmin can access all stores
            // This endpoint requires a storeId parameter - removed for now as it's not in the method signature
            return BadRequest("This endpoint requires store filtering which has been removed. Please use a different endpoint.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving store inventory", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific product assembly by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductAssemblyResponse>> GetProductAssembly(int id)
    {
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.CreatedByUser)
            .Include(pa => pa.CompletedByUser)
            .Include(pa => pa.Store)
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.RawProduct)
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.Warehouse)
            .FirstOrDefaultAsync(pa => pa.Id == id);

        if (assembly == null)
        {
            return NotFound();
        }

        var billOfMaterials = new List<BillOfMaterialResponse>();
        foreach (var bom in assembly.BillOfMaterials)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == bom.RawProductId && pi.WarehouseId == bom.WarehouseId);
            var availableQuantity = inventory?.Quantity ?? 0;
            
            billOfMaterials.Add(new BillOfMaterialResponse
            {
                Id = bom.Id,
                RawProductId = bom.RawProductId,
                RawProductName = bom.RawProduct.Name,
                RawProductSKU = bom.RawProduct.SKU ?? "",
                WarehouseId = bom.WarehouseId,
                WarehouseName = bom.Warehouse.Name,
                RequiredQuantity = bom.RequiredQuantity,
                AvailableQuantity = availableQuantity,
                ShortageQuantity = bom.RequiredQuantity - availableQuantity,
                Unit = bom.Unit,
                Notes = bom.Notes,
                IsAvailable = availableQuantity >= bom.RequiredQuantity
            });
        }

        var response = new ProductAssemblyResponse
        {
            Id = assembly.Id,
            Name = assembly.Name,
            Description = assembly.Description,
            Quantity = assembly.Quantity,
            Unit = assembly.Unit,
            Instructions = assembly.Instructions,
            Status = assembly.Status,
            Notes = assembly.Notes,
            CreatedByUserId = assembly.CreatedByUserId,
            CreatedByUserName = assembly.CreatedByUser.FullName,
            CompletedByUserId = assembly.CompletedByUserId,
            CompletedByUserName = assembly.CompletedByUser?.FullName,
            CreatedAt = assembly.CreatedAt,
            UpdatedAt = assembly.UpdatedAt,
            StartedAt = assembly.StartedAt,
            CompletedAt = assembly.CompletedAt,
            StoreId = assembly.StoreId,
            StoreName = assembly.Store?.Name,
            IsActive = assembly.IsActive,
            SalePrice = assembly.SalePrice,
            BillOfMaterials = billOfMaterials
        };

        // Calculate CanStart and ValidationMessage after BillOfMaterials is populated
        response.CanStart = assembly.Status == "Pending" && response.BillOfMaterials.All(bom => bom.IsAvailable);
        response.ValidationMessage = assembly.Status != "Pending" ? "Assembly is not in pending status" :
            response.BillOfMaterials.Any(bom => !bom.IsAvailable) ? "Insufficient materials available" : null;

        return Ok(response);
    }

    /// <summary>
    /// Get active assemblies for offers (Admin only)
    /// </summary>
    [HttpGet("pos-offers")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<POSOfferResponse>>> GetPOSOffers([FromQuery] int? storeId = null)
    {
        // Store filtering removed - SuperAdmin can access any store
        // If storeId not provided, return all active completed assemblies
        if (storeId == null)
        {
            // Return all active completed assemblies for SuperAdmin
        }

        var assemblies = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.RawProduct)
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.Warehouse)
            .Where(pa => pa.StoreId == storeId && pa.IsActive && pa.Status == "Completed")
            .ToListAsync();

        var offers = new List<POSOfferResponse>();
        foreach (var pa in assemblies)
        {
            var items = new List<POSOfferItemResponse>();
            var billOfMaterials = new List<BillOfMaterialResponse>();
            
            foreach (var bom in pa.BillOfMaterials)
            {
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == bom.RawProductId && pi.WarehouseId == bom.WarehouseId);
                var availableQuantity = inventory?.Quantity ?? 0;
                
                items.Add(new POSOfferItemResponse
                {
                    ProductId = bom.RawProductId,
                    ProductName = bom.RawProduct.Name,
                    ProductSKU = bom.RawProduct.SKU ?? "",
                    Quantity = bom.RequiredQuantity,
                    Unit = bom.Unit,
                    UnitPrice = bom.RawProduct.Price,
                    TotalPrice = bom.RawProduct.Price * bom.RequiredQuantity,
                    WarehouseName = bom.Warehouse.Name,
                    IsAvailable = availableQuantity >= bom.RequiredQuantity,
                    Notes = bom.Notes
                });
                
                billOfMaterials.Add(new BillOfMaterialResponse
                {
                    Id = bom.Id,
                    RawProductId = bom.RawProductId,
                    RawProductName = bom.RawProduct.Name,
                    RawProductSKU = bom.RawProduct.SKU ?? "",
                    WarehouseId = bom.WarehouseId,
                    WarehouseName = bom.Warehouse.Name,
                    RequiredQuantity = bom.RequiredQuantity,
                    AvailableQuantity = availableQuantity,
                    ShortageQuantity = bom.RequiredQuantity - availableQuantity,
                    Unit = bom.Unit,
                    Notes = bom.Notes,
                    IsAvailable = availableQuantity >= bom.RequiredQuantity
                });
            }
            
            offers.Add(new POSOfferResponse
            {
                Id = pa.Id,
                Name = pa.Name,
                Description = pa.Description,
                Quantity = pa.Quantity,
                Unit = pa.Unit,
                SalePrice = pa.SalePrice,
                IsAvailable = items.All(item => item.IsAvailable),
                AvailabilityMessage = items.Any(item => !item.IsAvailable) ? "Insufficient materials" : "Available",
                Items = items,
                BillOfMaterials = billOfMaterials
            });
        }

        return Ok(offers);
    }

    /// <summary>
    /// Process an assembly sale (Admin only)
    /// </summary>
    [HttpPost("pos-sale")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<POSAssemblySaleResponse>> ProcessPOSAssemblySale(POSAssemblySaleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        // Store filtering removed - SuperAdmin can access any store
        // Get the assembly (storeId comes from the assembly itself)
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.RawProduct)
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.Warehouse)
            .FirstOrDefaultAsync(pa => pa.Id == request.AssemblyId && pa.IsActive);
        
        if (assembly == null)
        {
            return NotFound("Assembly not found");
        }
        
        if (assembly.StoreId == null)
        {
            return BadRequest("Assembly does not have a store assigned");
        }
        var storeId = assembly.StoreId.Value;

        if (assembly == null)
        {
            return NotFound("Assembly not found or not available for this store");
        }

        if (assembly.Status != "Completed")
        {
            return BadRequest("Assembly must be completed before it can be sold");
        }

        // Validate materials are available
        var shortages = new List<MaterialShortageResponse>();
        foreach (var bom in assembly.BillOfMaterials)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == bom.RawProductId && pi.WarehouseId == bom.WarehouseId);

            // CRITICAL FIX: Calculate total required quantity (per unit × assembly quantity)
            var totalRequiredQuantity = bom.RequiredQuantity * assembly.Quantity;

            if (inventory == null || inventory.Quantity < totalRequiredQuantity)
            {
                shortages.Add(new MaterialShortageResponse
                {
                    RawProductId = bom.RawProductId,
                    RawProductName = bom.RawProduct?.Name ?? "Unknown",
                    WarehouseId = bom.WarehouseId,
                    WarehouseName = bom.Warehouse?.Name ?? "Unknown",
                    RequiredQuantity = totalRequiredQuantity,
                    AvailableQuantity = inventory?.Quantity ?? 0,
                    ShortageQuantity = totalRequiredQuantity - (inventory?.Quantity ?? 0),
                    Unit = bom.Unit
                });
            }
        }

        if (shortages.Any())
        {
            return BadRequest(new { message = "Cannot process sale: insufficient materials available.", shortages });
        }

        // Deduct materials from inventory
        foreach (var bom in assembly.BillOfMaterials)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == bom.RawProductId && pi.WarehouseId == bom.WarehouseId);
            
            if (inventory != null)
            {
                // CRITICAL FIX: Multiply by assembly quantity
                var totalRequiredQuantity = bom.RequiredQuantity * assembly.Quantity;
                inventory.Quantity -= totalRequiredQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                _context.ProductInventories.Update(inventory);
                await _auditService.LogAsync("ProductInventory", inventory.Id.ToString(), "Updated", 
                    $"Quantity reduced by {totalRequiredQuantity} for assembly sale {assembly.Id} (Required: {bom.RequiredQuantity} x Assembly Qty: {assembly.Quantity})", 
                    System.Text.Json.JsonSerializer.Serialize(inventory), currentUserId);
            }
        }

        // Create sales order for the assembly
        var saleNumber = $"ASM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var salesOrder = new SalesOrder
        {
            OrderNumber = saleNumber,
            CustomerName = request.CustomerName ?? "Walk-in Customer",
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            CustomerAddress = request.CustomerAddress,
            OrderDate = DateTime.UtcNow,
            DeliveryDate = DateTime.UtcNow,
            TotalAmount = assembly.SalePrice ?? 0,
            Status = "Completed",
            PaymentStatus = "Paid",
            Notes = $"Assembly Sale: {assembly.Name} (Payment: {request.PaymentMethod})",
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        // Create sales item for the assembly
        var salesItem = new SalesItem
        {
            SalesOrderId = salesOrder.Id,
            ProductId = assembly.Id, // Using assembly ID as product ID for assembly sales
            WarehouseId = storeId,
            Quantity = 1, // Always 1 assembly sold
            UnitPrice = assembly.SalePrice ?? 0,
            TotalPrice = assembly.SalePrice ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.SalesItems.Add(salesItem);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", salesOrder.Id.ToString(), "Created", 
            null, System.Text.Json.JsonSerializer.Serialize(salesOrder), currentUserId);

        var response = new POSAssemblySaleResponse
        {
            SaleId = salesOrder.Id,
            SaleNumber = salesOrder.OrderNumber,
            AssemblyName = assembly.Name,
            AssemblyId = assembly.Id,
            TotalAmount = salesOrder.TotalAmount,
            ItemsSold = assembly.BillOfMaterials.Select(bom => new POSOfferItemResponse
            {
                ProductId = bom.RawProductId,
                ProductName = bom.RawProduct.Name,
                ProductSKU = bom.RawProduct.SKU ?? "",
                Quantity = bom.RequiredQuantity,
                Unit = bom.Unit,
                UnitPrice = bom.RawProduct.Price,
                TotalPrice = bom.RawProduct.Price * bom.RequiredQuantity,
                WarehouseName = bom.Warehouse.Name,
                IsAvailable = true,
                Notes = bom.Notes
            }).ToList(),
            SaleDate = salesOrder.OrderDate,
            CashierName = currentUser.FullName
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new product assembly (simplified for store managers)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductAssemblyResponse>> CreateProductAssembly(CreateProductAssemblyRequest request)
    {
        if (request.BillOfMaterials == null || !request.BillOfMaterials.Any())
        {
            return BadRequest("Product assembly must contain at least one material.");
        }

        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);

        // Determine store ID based on user role
        int? storeId = null;
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // Only SuperAdmin can create assemblies
        if (!userRoles.Contains("SuperAdmin"))
        {
            return StatusCode(403, new { Message = "You don't have permission to create product assemblies" });
        }
        
        storeId = request.StoreId;
        if (storeId == null)
        {
            return BadRequest("Store ID is required when creating assemblies.");
        }

        if (storeId == null)
        {
            return BadRequest("Store ID is required for creating assembly offers.");
        }

        var assembly = new ProductAssembly
        {
            Name = request.Name,
            Description = request.Description,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Instructions = request.Instructions,
            Notes = request.Notes,
            CreatedByUserId = currentUserId,
            StoreId = storeId,
            IsActive = request.IsActive,
            SalePrice = request.SalePrice,
            Status = "Pending"
        };

        // Simplified bill of materials - automatically use store's warehouse
        foreach (var bomRequest in request.BillOfMaterials)
        {
            // Validate raw product exists
            var rawProduct = await _context.Products.FindAsync(bomRequest.RawProductId);
            if (rawProduct == null)
            {
                return BadRequest($"Product with ID {bomRequest.RawProductId} not found.");
            }

            // Check if product exists in store inventory
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == bomRequest.RawProductId && pi.WarehouseId == storeId);

            if (inventory == null)
            {
                return BadRequest($"Product '{rawProduct.Name}' is not available in your store inventory.");
            }

            var availableQuantity = inventory.Quantity;

            var bom = new BillOfMaterial
            {
                RawProductId = bomRequest.RawProductId,
                WarehouseId = storeId.Value, // Force use of store's warehouse
                RequiredQuantity = bomRequest.RequiredQuantity,
                AvailableQuantity = availableQuantity,
                Unit = bomRequest.Unit ?? rawProduct.Unit,
                Notes = bomRequest.Notes
            };

            assembly.BillOfMaterials.Add(bom);
        }

        _context.ProductAssemblies.Add(assembly);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Created", $"Created product assembly {assembly.Name}", null, currentUserId);

        return CreatedAtAction(nameof(GetProductAssembly), new { id = assembly.Id }, await GetProductAssembly(assembly.Id));
    }

    /// <summary>
    /// Create a simple assembly offer (simplified for store managers)
    /// </summary>
    [HttpPost("create-offer")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductAssemblyResponse>> CreateAssemblyOffer(CreateAssemblyOfferRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest("Assembly offer must contain at least one item.");
        }

        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        // Determine store ID based on user role
        int? storeId = null;
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        if (userRoles.Contains("SuperAdmin"))
        {
            // SuperAdmin can create assembly offers for any store
            storeId = request.StoreId;
            if (storeId == null)
            {
                return BadRequest("Store ID is required when creating assembly offers as SuperAdmin.");
            }
        }
        else if (userRoles.Contains("StoreManager"))
        {
            // StoreManager can only create assembly offers for their own store
            // Store filtering removed - SuperAdmin can create for any store
            storeId = request.StoreId;
            if (storeId == null)
            {
                return BadRequest("Store ID is required when creating assembly offers.");
            }
        }
        else
        {
            return StatusCode(403, new { Message = "You don't have permission to create assembly offers" });
        }

        // Calculate total individual price for comparison
        decimal totalIndividualPrice = 0;
        foreach (var item in request.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                return BadRequest($"Product with ID {item.ProductId} not found.");
            }

            // Check if product exists in store inventory
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == storeId);

            if (inventory == null)
            {
                return BadRequest($"Product '{product.Name}' is not available in your store inventory.");
            }

            if (inventory.Quantity < item.Quantity)
            {
                return BadRequest($"Insufficient quantity for '{product.Name}'. Available: {inventory.Quantity}, Required: {item.Quantity}");
            }

            totalIndividualPrice += product.Price * item.Quantity;
        }

        // If no custom price set, use total individual price
        var salePrice = request.SalePrice ?? totalIndividualPrice;

        var assembly = new ProductAssembly
        {
            Name = request.Name,
            Description = request.Description ?? $"Combo offer: {string.Join(", ", request.Items.Select(i => $"{i.Quantity}x {_context.Products.Find(i.ProductId)?.Name}"))}",
            Quantity = 1, // Always 1 offer
            Unit = "offer",
            Instructions = "Package items together as a combo offer",
            Notes = $"Individual total: ${totalIndividualPrice:F2}, Offer price: ${salePrice:F2}, Savings: ${totalIndividualPrice - salePrice:F2}",
            CreatedByUserId = currentUserId,
            StoreId = storeId,
            IsActive = true,
            SalePrice = salePrice,
            Status = "Pending"
        };

        // Create bill of materials from items
        foreach (var item in request.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == storeId);

            var bom = new BillOfMaterial
            {
                RawProductId = item.ProductId,
                WarehouseId = storeId.Value,
                RequiredQuantity = item.Quantity,
                AvailableQuantity = inventory!.Quantity,
                Unit = product!.Unit,
                Notes = item.Notes
            };

            assembly.BillOfMaterials.Add(bom);
        }

        _context.ProductAssemblies.Add(assembly);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Created", 
            $"Created assembly offer '{assembly.Name}' with {request.Items.Count} items for ${salePrice:F2}", null, currentUserId);

        return CreatedAtAction(nameof(GetProductAssembly), new { id = assembly.Id }, await GetProductAssembly(assembly.Id));
    }

    /// <summary>
    /// Suggest multiple assembly offers based on inventory levels
    /// </summary>
    [HttpPost("suggest-offers")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<SuggestedOfferResponse>>> SuggestMultipleOffers(SuggestOffersRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        // Determine store ID based on user role
        int? storeId = null;
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        if (userRoles.Contains("SuperAdmin"))
        {
            storeId = request.StoreId;
            if (storeId == null)
            {
                return BadRequest("Store ID is required when suggesting offers as SuperAdmin.");
            }
        }
        else if (userRoles.Contains("StoreManager"))
        {
            // Store filtering removed - SuperAdmin can access any store
            storeId = request.StoreId;
            if (storeId == null)
            {
                return BadRequest("Store ID is required.");
            }
        }
        else
        {
            return StatusCode(403, new { Message = "You don't have permission to suggest offers" });
        }

        var suggestions = new List<SuggestedOfferResponse>();

        // Get all products with inventory in the store
        var productsWithInventory = await _context.ProductInventories
            .Where(pi => pi.WarehouseId == storeId && pi.Quantity > 0)
            .Include(pi => pi.Product)
            .ThenInclude(p => p.Category)
            .ToListAsync();

        if (!productsWithInventory.Any())
        {
            return Ok(suggestions); // No inventory available
        }

        // Group products by category for better offer suggestions
        var productsByCategory = productsWithInventory
            .GroupBy(pi => pi.Product.Category.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Suggest offers based on different strategies
        foreach (var category in productsByCategory)
        {
            var categoryProducts = category.Value.OrderByDescending(pi => pi.Quantity).ToList();
            
            // Strategy 1: Single category offers (2-3 items from same category)
            if (categoryProducts.Count >= 2)
            {
                var offer1 = CreateSuggestedOffer(categoryProducts.Take(2).ToList(), "Same Category Combo", category.Key);
                if (offer1 != null) suggestions.Add(offer1);
            }
            
            if (categoryProducts.Count >= 3)
            {
                var offer2 = CreateSuggestedOffer(categoryProducts.Take(3).ToList(), "Premium Category Pack", category.Key);
                if (offer2 != null) suggestions.Add(offer2);
            }

            // Strategy 2: Mixed category offers (if we have multiple categories)
            if (productsByCategory.Count > 1)
            {
                var mixedProducts = new List<ProductInventory>();
                foreach (var cat in productsByCategory.Take(2))
                {
                    mixedProducts.Add(cat.Value.First());
                }
                
                var mixedOffer = CreateSuggestedOffer(mixedProducts, "Mixed Category Special", "Mixed");
                if (mixedOffer != null) suggestions.Add(mixedOffer);
            }
        }

        // Strategy 3: High-value offers (most expensive items)
        var highValueProducts = productsWithInventory
            .OrderByDescending(pi => pi.Product.Price)
            .Take(3)
            .ToList();
            
        if (highValueProducts.Count >= 2)
        {
            var luxuryOffer = CreateSuggestedOffer(highValueProducts.Take(2).ToList(), "Luxury Collection", "Luxury");
            if (luxuryOffer != null) suggestions.Add(luxuryOffer);
        }

        // Strategy 4: High inventory offers (items with most stock)
        var highStockProducts = productsWithInventory
            .OrderByDescending(pi => pi.Quantity)
            .Take(3)
            .ToList();
            
        if (highStockProducts.Count >= 2)
        {
            var stockOffer = CreateSuggestedOffer(highStockProducts.Take(2).ToList(), "High Stock Special", "High Stock");
            if (stockOffer != null) suggestions.Add(stockOffer);
        }

        return Ok(suggestions.Take(request.MaxSuggestions ?? 10).ToList());
    }

    private SuggestedOfferResponse? CreateSuggestedOffer(List<ProductInventory> products, string offerType, string category)
    {
        if (products.Count < 2) return null;

        var items = new List<SuggestedOfferItem>();
        decimal totalIndividualPrice = 0;
        int maxQuantity = int.MaxValue;

        foreach (var product in products)
        {
            // Calculate how many offers we can make based on inventory
            var availableQuantity = (int)product.Quantity;
            var suggestedQuantity = Math.Min(availableQuantity, 2); // Suggest max 2 per item
            
            maxQuantity = Math.Min(maxQuantity, availableQuantity);
            
            items.Add(new SuggestedOfferItem
            {
                ProductId = product.ProductId,
                ProductName = product.Product.Name,
                SuggestedQuantity = suggestedQuantity,
                UnitPrice = product.Product.Price,
                AvailableQuantity = availableQuantity,
                Category = product.Product.Category.Name
            });

            totalIndividualPrice += product.Product.Price * suggestedQuantity;
        }

        // Calculate suggested offer price (10-20% discount)
        var discountPercentage = 0.15m; // 15% discount
        var suggestedPrice = totalIndividualPrice * (1 - discountPercentage);
        var savings = totalIndividualPrice - suggestedPrice;

        return new SuggestedOfferResponse
        {
            OfferName = $"{offerType} - {category}",
            Description = $"Combo offer with {items.Count} items from {category}",
            Items = items,
            IndividualTotalPrice = totalIndividualPrice,
            SuggestedPrice = suggestedPrice,
            Savings = savings,
            DiscountPercentage = discountPercentage * 100,
            MaxQuantityAvailable = maxQuantity,
            EstimatedProfitMargin = CalculateProfitMargin(suggestedPrice, items)
        };
    }

    private decimal CalculateProfitMargin(decimal suggestedPrice, List<SuggestedOfferItem> items)
    {
        // Simple profit margin calculation (assuming 60% cost)
        var estimatedCost = suggestedPrice * 0.6m;
        return ((suggestedPrice - estimatedCost) / suggestedPrice) * 100;
    }

    /// <summary>
    /// Update a product assembly
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateProductAssembly(int id, UpdateProductAssemblyRequest request)
    {
        var assembly = await _context.ProductAssemblies.FindAsync(id);
        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status == "Completed" || assembly.Status == "Cancelled")
        {
            return BadRequest("Cannot update a product assembly that has been completed or cancelled.");
        }

        var currentUserId = GetCurrentUserId();

        assembly.Name = request.Name ?? assembly.Name;
        assembly.Description = request.Description ?? assembly.Description;
        assembly.Quantity = request.Quantity ?? assembly.Quantity;
        assembly.Unit = request.Unit ?? assembly.Unit;
        assembly.Instructions = request.Instructions ?? assembly.Instructions;
        assembly.Status = request.Status ?? assembly.Status;
        assembly.Notes = request.Notes ?? assembly.Notes;
        assembly.UpdatedAt = DateTime.UtcNow;

        _context.Entry(assembly).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Updated", $"Updated product assembly {assembly.Name}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Validate if assembly can be started
    /// </summary>
    [HttpGet("{id}/validate")]
    public async Task<ActionResult<AssemblyValidationResponse>> ValidateAssembly(int id)
    {
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
            .FirstOrDefaultAsync(pa => pa.Id == id);

        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status != "Pending")
        {
            return Ok(new AssemblyValidationResponse
            {
                CanStart = false,
                Message = "Assembly is not in pending status"
            });
        }

        var shortages = new List<MaterialShortageResponse>();

        foreach (var bom in assembly.BillOfMaterials)
        {
            // CRITICAL FIX: Calculate total required quantity (per unit × assembly quantity)
            var totalRequiredQuantity = bom.RequiredQuantity * assembly.Quantity;
            
            if (bom.AvailableQuantity < totalRequiredQuantity)
            {
                shortages.Add(new MaterialShortageResponse
                {
                    RawProductId = bom.RawProductId,
                    RawProductName = bom.RawProduct?.Name ?? "Unknown",
                    WarehouseId = bom.WarehouseId,
                    WarehouseName = bom.Warehouse?.Name ?? "Unknown",
                    RequiredQuantity = totalRequiredQuantity,
                    AvailableQuantity = bom.AvailableQuantity,
                    ShortageQuantity = totalRequiredQuantity - bom.AvailableQuantity,
                    Unit = bom.Unit
                });
            }
        }

        return Ok(new AssemblyValidationResponse
        {
            CanStart = !shortages.Any(),
            Shortages = shortages,
            Message = shortages.Any() ? "Insufficient materials available" : "Assembly can be started"
        });
    }

    /// <summary>
    /// Start product assembly
    /// </summary>
    [HttpPost("{id}/start")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> StartAssembly(int id, StartAssemblyRequest request)
    {
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.RawProduct)
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.Warehouse)
            .FirstOrDefaultAsync(pa => pa.Id == id);

        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status != "Pending")
        {
            return BadRequest("Only pending assemblies can be started.");
        }

        // Validate materials are available
        var shortages = new List<MaterialShortageResponse>();
        foreach (var bom in assembly.BillOfMaterials)
        {
            // CRITICAL FIX: Calculate total required quantity (per unit × assembly quantity)
            var totalRequiredQuantity = bom.RequiredQuantity * assembly.Quantity;
            
            if (bom.AvailableQuantity < totalRequiredQuantity)
            {
                shortages.Add(new MaterialShortageResponse
                {
                    RawProductId = bom.RawProductId,
                    RawProductName = bom.RawProduct?.Name ?? "Unknown",
                    WarehouseId = bom.WarehouseId,
                    WarehouseName = bom.Warehouse?.Name ?? "Unknown",
                    RequiredQuantity = totalRequiredQuantity,
                    AvailableQuantity = bom.AvailableQuantity,
                    ShortageQuantity = totalRequiredQuantity - bom.AvailableQuantity,
                    Unit = bom.Unit
                });
            }
        }

        if (shortages.Any())
        {
            return BadRequest("Cannot start assembly: insufficient materials available.");
        }

        var currentUserId = GetCurrentUserId();

        assembly.Status = "InProgress";
        assembly.StartedAt = DateTime.UtcNow;
        assembly.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            assembly.Notes = string.IsNullOrEmpty(assembly.Notes) ? request.Notes : $"{assembly.Notes}\nStart Notes: {request.Notes}";
        }

        _context.Entry(assembly).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Started", $"Started product assembly {assembly.Name}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Complete product assembly and update inventory
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CompleteAssembly(int id, CompleteAssemblyRequest request)
    {
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
            .FirstOrDefaultAsync(pa => pa.Id == id);

        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status != "InProgress")
        {
            return BadRequest("Only in-progress assemblies can be completed.");
        }

        var currentUserId = GetCurrentUserId();

        // Update assembly status
        assembly.Status = "Completed";
        assembly.CompletedByUserId = currentUserId;
        assembly.CompletedAt = DateTime.UtcNow;
        assembly.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            assembly.Notes = string.IsNullOrEmpty(assembly.Notes) ? request.Notes : $"{assembly.Notes}\nCompletion Notes: {request.Notes}";
        }

        // Deduct raw materials from inventory
        foreach (var bom in assembly.BillOfMaterials)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == bom.RawProductId && pi.WarehouseId == bom.WarehouseId);

            if (inventory != null)
            {
                // CRITICAL FIX: Multiply by assembly quantity
                var totalRequiredQuantity = bom.RequiredQuantity * assembly.Quantity;
                inventory.Quantity -= totalRequiredQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                
                await _auditService.LogAsync("ProductInventory", inventory.Id.ToString(), "Updated", 
                    $"Quantity reduced by {totalRequiredQuantity} for assembly completion {assembly.Id} (Required: {bom.RequiredQuantity} x Assembly Qty: {assembly.Quantity})", 
                    System.Text.Json.JsonSerializer.Serialize(inventory), currentUserId);
            }
        }

        // Create or update final product inventory
        // Note: This assumes the final product is already in the Products table
        // You might want to create the final product automatically or require it to exist
        var finalProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Name == assembly.Name);

        if (finalProduct != null)
        {
            // Use the assembly's store ID to place the final product in the correct store
            var targetStoreId = assembly.StoreId;
            
            // If no store is specified, use the first warehouse as fallback
            if (targetStoreId == null)
            {
                var fallbackWarehouse = await _context.Warehouses.FirstOrDefaultAsync();
                targetStoreId = fallbackWarehouse?.Id;
            }
            
            if (targetStoreId.HasValue)
            {
                var finalProductInventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == finalProduct.Id && pi.WarehouseId == targetStoreId.Value);

                if (finalProductInventory == null)
                {
                    finalProductInventory = new ProductInventory
                    {
                        ProductId = finalProduct.Id,
                        WarehouseId = targetStoreId.Value,
                        Quantity = assembly.Quantity,
                        Unit = assembly.Unit,
                        MinimumStockLevel = 0,
                        MaximumStockLevel = 1000,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ProductInventories.Add(finalProductInventory);
                }
                else
                {
                    finalProductInventory.Quantity += assembly.Quantity;
                    finalProductInventory.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Completed", $"Completed product assembly {assembly.Name} and updated inventory", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Cancel product assembly
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAssembly(int id)
    {
        var assembly = await _context.ProductAssemblies.FindAsync(id);
        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status == "Completed" || assembly.Status == "Cancelled")
        {
            return BadRequest("Cannot cancel a product assembly that has been completed or is already cancelled.");
        }

        var currentUserId = GetCurrentUserId();
        assembly.Status = "Cancelled";
        assembly.UpdatedAt = DateTime.UtcNow;

        _context.Entry(assembly).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Cancelled", $"Cancelled product assembly {assembly.Name}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Delete a product assembly (only if it's pending)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProductAssembly(int id)
    {
        var assembly = await _context.ProductAssemblies.FindAsync(id);
        if (assembly == null)
        {
            return NotFound();
        }

        if (assembly.Status != "Pending")
        {
            return BadRequest("Only pending product assemblies can be deleted.");
        }

        var currentUserId = GetCurrentUserId();

        _context.ProductAssemblies.Remove(assembly);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductAssembly", assembly.Id.ToString(), "Deleted", $"Deleted product assembly {assembly.Name}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Get assemblies by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ProductAssemblyListResponse>>> GetAssembliesByStatus(string status)
    {
        var assemblies = await _context.ProductAssemblies
            .Where(pa => pa.Status == status)
            .Include(pa => pa.CreatedByUser)
            .Include(pa => pa.CompletedByUser)
            .Include(pa => pa.BillOfMaterials)
            .OrderByDescending(pa => pa.CreatedAt)
            .Select(pa => new ProductAssemblyListResponse
            {
                Id = pa.Id,
                Name = pa.Name,
                Description = pa.Description,
                Quantity = pa.Quantity,
                Unit = pa.Unit,
                Status = pa.Status,
                CreatedByUserName = pa.CreatedByUser.FullName,
                CompletedByUserName = pa.CompletedByUser != null ? pa.CompletedByUser.FullName : null,
                CreatedAt = pa.CreatedAt,
                CompletedAt = pa.CompletedAt,
                MaterialCount = pa.BillOfMaterials.Count,
                CanStart = pa.Status == "Pending" && pa.BillOfMaterials.All(bom => bom.AvailableQuantity >= bom.RequiredQuantity)
            })
            .ToListAsync();

        return Ok(assemblies);
    }

    /// <summary>
    /// Get assembly report
    /// </summary>
    [HttpGet("report")]
    public async Task<ActionResult<AssemblyReportResponse>> GetAssemblyReport(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        var assemblies = await _context.ProductAssemblies
            .Where(pa => pa.CreatedAt >= startDate && pa.CreatedAt <= endDate)
            .Include(pa => pa.CreatedByUser)
            .Include(pa => pa.CompletedByUser)
            .ToListAsync();

        var report = new AssemblyReportResponse
        {
            FromDate = startDate,
            ToDate = endDate,
            TotalAssemblies = assemblies.Count,
            PendingAssemblies = assemblies.Count(a => a.Status == "Pending"),
            InProgressAssemblies = assemblies.Count(a => a.Status == "InProgress"),
            CompletedAssemblies = assemblies.Count(a => a.Status == "Completed"),
            CancelledAssemblies = assemblies.Count(a => a.Status == "Cancelled"),
            TotalQuantityProduced = assemblies.Where(a => a.Status == "Completed").Sum(a => a.Quantity),
            RecentAssemblies = assemblies
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(pa => new ProductAssemblyListResponse
                {
                    Id = pa.Id,
                    Name = pa.Name,
                    Description = pa.Description,
                    Quantity = pa.Quantity,
                    Unit = pa.Unit,
                    Status = pa.Status,
                    CreatedByUserName = pa.CreatedByUser.FullName,
                    CompletedByUserName = pa.CompletedByUser != null ? pa.CompletedByUser.FullName : null,
                    CreatedAt = pa.CreatedAt,
                    CompletedAt = pa.CompletedAt,
                    MaterialCount = 0, // Will be populated if needed
                    CanStart = false // Will be calculated if needed
                })
                .ToList()
        };

        return Ok(report);
    }

    /// <summary>
    /// Get assembly cost analysis
    /// </summary>
    [HttpGet("{id}/cost-analysis")]
    public async Task<ActionResult<AssemblyCostAnalysisResponse>> GetAssemblyCostAnalysis(int id)
    {
        var assembly = await _context.ProductAssemblies
            .Include(pa => pa.BillOfMaterials)
                .ThenInclude(bom => bom.RawProduct)
            .FirstOrDefaultAsync(pa => pa.Id == id);

        if (assembly == null)
        {
            return NotFound();
        }

        var materialCosts = assembly.BillOfMaterials.Select(bom => new MaterialCostResponse
        {
            RawProductId = bom.RawProductId,
            RawProductName = bom.RawProduct.Name,
            RequiredQuantity = bom.RequiredQuantity,
            UnitCost = bom.RawProduct.Price,
            TotalCost = bom.RequiredQuantity * bom.RawProduct.Price,
            Unit = bom.Unit
        }).ToList();

        var totalCost = materialCosts.Sum(mc => mc.TotalCost);

        var response = new AssemblyCostAnalysisResponse
        {
            AssemblyId = assembly.Id,
            AssemblyName = assembly.Name,
            Quantity = assembly.Quantity,
            Unit = assembly.Unit,
            TotalCost = totalCost,
            CostPerUnit = assembly.Quantity > 0 ? totalCost / assembly.Quantity : 0,
            MaterialCosts = materialCosts
        };

        return Ok(response);
    }
}
