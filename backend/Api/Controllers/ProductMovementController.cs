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
public class ProductMovementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ProductMovementController(ApplicationDbContext context, IAuditService auditService)
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
    /// Get product movement report with filtering and pagination
    /// </summary>
    [HttpPost("report")]
    public async Task<ActionResult<ProductMovementReportResponse>> GetProductMovementReport(ProductMovementReportRequest request)
    {
        var query = _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Include(pm => pm.CreatedByUser)
            .AsQueryable();

        // Apply filters
        if (request.FromDate.HasValue)
            query = query.Where(pm => pm.MovementDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(pm => pm.MovementDate <= request.ToDate.Value);

        if (request.ProductId.HasValue)
            query = query.Where(pm => pm.ProductId == request.ProductId.Value);

        if (request.WarehouseId.HasValue)
            query = query.Where(pm => pm.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrEmpty(request.MovementType))
            query = query.Where(pm => pm.MovementType == request.MovementType);

        if (!string.IsNullOrEmpty(request.Direction))
            query = query.Where(pm => pm.Direction == request.Direction);

        // Get total count for pagination
        var totalRecords = await query.CountAsync();

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "movementdate" => request.SortDirection == "asc" ? query.OrderBy(pm => pm.MovementDate) : query.OrderByDescending(pm => pm.MovementDate),
            "productname" => request.SortDirection == "asc" ? query.OrderBy(pm => pm.Product.Name) : query.OrderByDescending(pm => pm.Product.Name),
            "warehousename" => request.SortDirection == "asc" ? query.OrderBy(pm => pm.Warehouse.Name) : query.OrderByDescending(pm => pm.Warehouse.Name),
            "quantity" => request.SortDirection == "asc" ? query.OrderBy(pm => pm.Quantity) : query.OrderByDescending(pm => pm.Quantity),
            _ => query.OrderByDescending(pm => pm.MovementDate)
        };

        // Apply pagination
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? 50;
        var movements = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(pm => new ProductMovementResponse
            {
                Id = pm.Id,
                ProductId = pm.ProductId,
                ProductName = pm.Product.Name,
                ProductSKU = pm.Product.SKU ?? "",
                WarehouseId = pm.WarehouseId,
                WarehouseName = pm.Warehouse.Name,
                MovementType = pm.MovementType,
                Quantity = pm.Quantity,
                Unit = pm.Unit,
                Direction = pm.Direction,
                Description = pm.Description,
                ReferenceNumber = pm.ReferenceNumber,
                ReferenceId = pm.ReferenceId,
                ReferenceType = pm.ReferenceType,
                CreatedByUserName = pm.CreatedByUser != null ? pm.CreatedByUser.FullName : null,
                MovementDate = pm.MovementDate,
                CreatedAt = pm.CreatedAt,
                Notes = pm.Notes
            })
            .ToListAsync();

        // Calculate summary
        var summaryQuery = _context.ProductMovements.AsQueryable();
        
        if (request.FromDate.HasValue)
            summaryQuery = summaryQuery.Where(pm => pm.MovementDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            summaryQuery = summaryQuery.Where(pm => pm.MovementDate <= request.ToDate.Value);
        if (request.ProductId.HasValue)
            summaryQuery = summaryQuery.Where(pm => pm.ProductId == request.ProductId.Value);
        if (request.WarehouseId.HasValue)
            summaryQuery = summaryQuery.Where(pm => pm.WarehouseId == request.WarehouseId.Value);
        if (!string.IsNullOrEmpty(request.MovementType))
            summaryQuery = summaryQuery.Where(pm => pm.MovementType == request.MovementType);
        if (!string.IsNullOrEmpty(request.Direction))
            summaryQuery = summaryQuery.Where(pm => pm.Direction == request.Direction);

        var totalIn = await summaryQuery.Where(pm => pm.Direction == "In").SumAsync(pm => pm.Quantity);
        var totalOut = await summaryQuery.Where(pm => pm.Direction == "Out").SumAsync(pm => pm.Quantity);

        var response = new ProductMovementReportResponse
        {
            FromDate = request.FromDate ?? DateTime.MinValue,
            ToDate = request.ToDate ?? DateTime.MaxValue,
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            MovementType = request.MovementType,
            Direction = request.Direction,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            Movements = movements,
            Summary = new ProductMovementSummaryResponse
            {
                ProductId = request.ProductId ?? 0,
                WarehouseId = request.WarehouseId ?? 0,
                TotalIn = totalIn,
                TotalOut = totalOut,
                NetMovement = totalIn - totalOut
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get product movement analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<ProductMovementAnalyticsResponse>> GetProductMovementAnalytics(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        var movements = await _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Where(pm => pm.MovementDate >= startDate && pm.MovementDate <= endDate)
            .ToListAsync();

        var analytics = new ProductMovementAnalyticsResponse
        {
            FromDate = startDate,
            ToDate = endDate,
            TotalMovements = movements.Count,
            TotalProducts = movements.Select(pm => pm.ProductId).Distinct().Count(),
            TotalWarehouses = movements.Select(pm => pm.WarehouseId).Distinct().Count(),
            TotalQuantityIn = movements.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity),
            TotalQuantityOut = movements.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity),
            NetMovement = movements.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity) - 
                         movements.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity),
            MovementTypeCounts = movements.GroupBy(pm => pm.MovementType)
                .ToDictionary(g => g.Key, g => g.Count()),
            MovementTypeQuantities = movements.GroupBy(pm => pm.MovementType)
                .ToDictionary(g => g.Key, g => g.Sum(pm => pm.Quantity)),
            TopMovingProducts = movements
                .GroupBy(pm => new { pm.ProductId, pm.WarehouseId })
                .Select(g => new ProductMovementSummaryResponse
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.First().Product.Name,
                    ProductSKU = g.First().Product.SKU ?? "",
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = g.First().Warehouse.Name,
                    TotalIn = g.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity),
                    TotalOut = g.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity),
                    NetMovement = g.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity) - 
                                 g.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity)
                })
                .OrderByDescending(pm => Math.Abs(pm.NetMovement))
                .Take(10)
                .ToList(),
            WarehouseActivity = movements
                .GroupBy(pm => pm.WarehouseId)
                .Select(g => new ProductMovementSummaryResponse
                {
                    WarehouseId = g.Key,
                    WarehouseName = g.First().Warehouse.Name,
                    TotalIn = g.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity),
                    TotalOut = g.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity),
                    NetMovement = g.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity) - 
                                 g.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity)
                })
                .OrderByDescending(pm => Math.Abs(pm.NetMovement))
                .ToList(),
            RecentMovements = movements
                .OrderByDescending(pm => pm.MovementDate)
                .Take(10)
                .Select(pm => new ProductMovementResponse
                {
                    Id = pm.Id,
                    ProductId = pm.ProductId,
                    ProductName = pm.Product.Name,
                    ProductSKU = pm.Product.SKU ?? "",
                    WarehouseId = pm.WarehouseId,
                    WarehouseName = pm.Warehouse.Name,
                    MovementType = pm.MovementType,
                    Quantity = pm.Quantity,
                    Unit = pm.Unit,
                    Direction = pm.Direction,
                    Description = pm.Description,
                    ReferenceNumber = pm.ReferenceNumber,
                    MovementDate = pm.MovementDate
                })
                .ToList()
        };

        return Ok(analytics);
    }

    /// <summary>
    /// Get product movement trend for a specific product and warehouse
    /// </summary>
    [HttpGet("trend")]
    public async Task<ActionResult<List<ProductMovementTrendResponse>>> GetProductMovementTrend(
        int productId, int warehouseId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        var movements = await _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Where(pm => pm.ProductId == productId && 
                        pm.WarehouseId == warehouseId && 
                        pm.MovementDate >= startDate && 
                        pm.MovementDate <= endDate)
            .ToListAsync();

        var dailyMovements = movements
            .GroupBy(pm => pm.MovementDate.Date)
            .Select(g => new ProductMovementTrendResponse
            {
                ProductId = productId,
                ProductName = g.First().Product.Name,
                WarehouseId = warehouseId,
                WarehouseName = g.First().Warehouse.Name,
                Date = g.Key,
                OpeningBalance = 0, // Would need to calculate from previous day
                Purchases = g.Where(pm => pm.MovementType == "Purchase" && pm.Direction == "In").Sum(pm => pm.Quantity),
                Sales = g.Where(pm => pm.MovementType == "Sale" && pm.Direction == "Out").Sum(pm => pm.Quantity),
                AssemblyProduction = g.Where(pm => pm.MovementType == "Assembly" && pm.Direction == "In").Sum(pm => pm.Quantity),
                AssemblyConsumption = g.Where(pm => pm.MovementType == "Assembly" && pm.Direction == "Out").Sum(pm => pm.Quantity),
                Transfers = g.Where(pm => pm.MovementType == "Transfer").Sum(pm => pm.Quantity),
                Adjustments = g.Where(pm => pm.MovementType == "Adjustment").Sum(pm => pm.Quantity),
                ClosingBalance = 0, // Would need to calculate
                NetMovement = g.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity) - 
                             g.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity)
            })
            .OrderBy(t => t.Date)
            .ToList();

        return Ok(dailyMovements);
    }

    /// <summary>
    /// Get available filters for product movement reports
    /// </summary>
    [HttpGet("filters")]
    public async Task<ActionResult<ProductMovementFilterResponse>> GetProductMovementFilters()
    {
        var products = await _context.ProductMovements
            .Include(pm => pm.Product)
            .GroupBy(pm => new { pm.ProductId, pm.Product.Name, pm.Product.SKU })
            .Select(g => new ProductFilterOption
            {
                Id = g.Key.ProductId,
                Name = g.Key.Name,
                SKU = g.Key.SKU ?? "",
                MovementCount = g.Count()
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        var warehouses = await _context.ProductMovements
            .Include(pm => pm.Warehouse)
            .GroupBy(pm => new { pm.WarehouseId, pm.Warehouse.Name })
            .Select(g => new WarehouseFilterOption
            {
                Id = g.Key.WarehouseId,
                Name = g.Key.Name,
                MovementCount = g.Count()
            })
            .OrderBy(w => w.Name)
            .ToListAsync();

        var movementTypes = await _context.ProductMovements
            .Select(pm => pm.MovementType)
            .Distinct()
            .OrderBy(mt => mt)
            .ToListAsync();

        var directions = await _context.ProductMovements
            .Select(pm => pm.Direction)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var minDate = await _context.ProductMovements.MinAsync(pm => pm.MovementDate);
        var maxDate = await _context.ProductMovements.MaxAsync(pm => pm.MovementDate);

        var response = new ProductMovementFilterResponse
        {
            Products = products,
            Warehouses = warehouses,
            MovementTypes = movementTypes,
            Directions = directions,
            MinDate = minDate,
            MaxDate = maxDate
        };

        return Ok(response);
    }

    /// <summary>
    /// Get product movement comparison between two periods
    /// </summary>
    [HttpGet("comparison")]
    public async Task<ActionResult<List<ProductMovementComparisonResponse>>> GetProductMovementComparison(
        DateTime currentFrom, DateTime currentTo, DateTime previousFrom, DateTime previousTo,
        int? productId = null, int? warehouseId = null)
    {
        var currentMovements = await _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Where(pm => pm.MovementDate >= currentFrom && pm.MovementDate <= currentTo)
            .Where(pm => !productId.HasValue || pm.ProductId == productId.Value)
            .Where(pm => !warehouseId.HasValue || pm.WarehouseId == warehouseId.Value)
            .ToListAsync();

        var previousMovements = await _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Where(pm => pm.MovementDate >= previousFrom && pm.MovementDate <= previousTo)
            .Where(pm => !productId.HasValue || pm.ProductId == productId.Value)
            .Where(pm => !warehouseId.HasValue || pm.WarehouseId == warehouseId.Value)
            .ToListAsync();

        var currentGroups = currentMovements.GroupBy(pm => new { pm.ProductId, pm.WarehouseId });
        var previousGroups = previousMovements.GroupBy(pm => new { pm.ProductId, pm.WarehouseId });

        var comparisons = new List<ProductMovementComparisonResponse>();

        foreach (var currentGroup in currentGroups)
        {
            var previousGroup = previousGroups.FirstOrDefault(pg => 
                pg.Key.ProductId == currentGroup.Key.ProductId && 
                pg.Key.WarehouseId == currentGroup.Key.WarehouseId);

            var currentPeriod = new ProductMovementPeriod
            {
                FromDate = currentFrom,
                ToDate = currentTo,
                TotalIn = currentGroup.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity),
                TotalOut = currentGroup.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity),
                MovementCount = currentGroup.Count(),
                AverageDailyMovement = (decimal)((double)currentGroup.Sum(pm => pm.Quantity) / (currentTo - currentFrom).TotalDays)
            };
            currentPeriod.NetMovement = currentPeriod.TotalIn - currentPeriod.TotalOut;

            var previousPeriod = new ProductMovementPeriod
            {
                FromDate = previousFrom,
                ToDate = previousTo,
                TotalIn = previousGroup?.Where(pm => pm.Direction == "In").Sum(pm => pm.Quantity) ?? 0,
                TotalOut = previousGroup?.Where(pm => pm.Direction == "Out").Sum(pm => pm.Quantity) ?? 0,
                MovementCount = previousGroup?.Count() ?? 0,
                AverageDailyMovement = (decimal)((double)(previousGroup?.Sum(pm => pm.Quantity) ?? 0) / (previousTo - previousFrom).TotalDays)
            };
            previousPeriod.NetMovement = previousPeriod.TotalIn - previousPeriod.TotalOut;

            var comparison = new ProductMovementComparison
            {
                InChange = previousPeriod.TotalIn > 0 ? 
                    ((currentPeriod.TotalIn - previousPeriod.TotalIn) / previousPeriod.TotalIn) * 100 : 0,
                OutChange = previousPeriod.TotalOut > 0 ? 
                    ((currentPeriod.TotalOut - previousPeriod.TotalOut) / previousPeriod.TotalOut) * 100 : 0,
                NetChange = previousPeriod.NetMovement != 0 ? 
                    (decimal)((double)(currentPeriod.NetMovement - previousPeriod.NetMovement) / (double)Math.Abs(previousPeriod.NetMovement)) * 100 : 0,
                CountChange = previousPeriod.MovementCount > 0 ? 
                    (int)(((currentPeriod.MovementCount - previousPeriod.MovementCount) / (double)previousPeriod.MovementCount) * 100) : 0,
                DailyAverageChange = previousPeriod.AverageDailyMovement > 0 ? 
                    ((currentPeriod.AverageDailyMovement - previousPeriod.AverageDailyMovement) / previousPeriod.AverageDailyMovement) * 100 : 0,
                Trend = currentPeriod.NetMovement > previousPeriod.NetMovement ? "Increasing" : 
                       currentPeriod.NetMovement < previousPeriod.NetMovement ? "Decreasing" : "Stable"
            };

            comparisons.Add(new ProductMovementComparisonResponse
            {
                ProductId = currentGroup.Key.ProductId,
                ProductName = currentGroup.First().Product.Name,
                WarehouseId = currentGroup.Key.WarehouseId,
                WarehouseName = currentGroup.First().Warehouse.Name,
                CurrentPeriod = currentPeriod,
                PreviousPeriod = previousPeriod,
                Comparison = comparison
            });
        }

        return Ok(comparisons.OrderByDescending(c => Math.Abs(c.Comparison.NetChange)));
    }

    /// <summary>
    /// Get product movement alerts (low stock, high movement, etc.)
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<List<ProductMovementAlertResponse>>> GetProductMovementAlerts()
    {
        var alerts = new List<ProductMovementAlertResponse>();

        // Get current inventory levels
        var inventories = await _context.ProductInventories
            .Include(pi => pi.Product)
            .Include(pi => pi.Warehouse)
            .ToListAsync();

        // Get recent movements (last 7 days)
        var recentMovements = await _context.ProductMovements
            .Where(pm => pm.MovementDate >= DateTime.UtcNow.AddDays(-7))
            .GroupBy(pm => new { pm.ProductId, pm.WarehouseId })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                WarehouseId = g.Key.WarehouseId,
                TotalMovement = g.Sum(pm => pm.Quantity),
                MovementCount = g.Count()
            })
            .ToListAsync();

        foreach (var inventory in inventories)
        {
            var recentMovement = recentMovements.FirstOrDefault(rm => 
                rm.ProductId == inventory.ProductId && rm.WarehouseId == inventory.WarehouseId);

            // Low stock alert
            if (inventory.MinimumStockLevel.HasValue && inventory.Quantity <= inventory.MinimumStockLevel.Value)
            {
                alerts.Add(new ProductMovementAlertResponse
                {
                    ProductId = inventory.ProductId,
                    ProductName = inventory.Product.Name,
                    WarehouseId = inventory.WarehouseId,
                    WarehouseName = inventory.Warehouse.Name,
                    AlertType = "LowStock",
                    Message = $"Stock level ({inventory.Quantity}) is below minimum threshold ({inventory.MinimumStockLevel})",
                    CurrentStock = inventory.Quantity,
                    MovementThreshold = inventory.MinimumStockLevel ?? 0,
                    AlertDate = DateTime.UtcNow,
                    Severity = inventory.Quantity <= 0 ? "Critical" : "High"
                });
            }

            // High movement alert
            if (recentMovement != null && recentMovement.TotalMovement > inventory.Quantity * 2)
            {
                alerts.Add(new ProductMovementAlertResponse
                {
                    ProductId = inventory.ProductId,
                    ProductName = inventory.Product.Name,
                    WarehouseId = inventory.WarehouseId,
                    WarehouseName = inventory.Warehouse.Name,
                    AlertType = "HighMovement",
                    Message = $"High movement detected: {recentMovement.TotalMovement} units in last 7 days",
                    CurrentStock = inventory.Quantity,
                    MovementThreshold = inventory.Quantity * 2,
                    AlertDate = DateTime.UtcNow,
                    Severity = "Medium"
                });
            }

            // No movement alert
            if (recentMovement == null && inventory.Quantity > 0)
            {
                alerts.Add(new ProductMovementAlertResponse
                {
                    ProductId = inventory.ProductId,
                    ProductName = inventory.Product.Name,
                    WarehouseId = inventory.WarehouseId,
                    WarehouseName = inventory.Warehouse.Name,
                    AlertType = "NoMovement",
                    Message = "No movement detected in the last 7 days",
                    CurrentStock = inventory.Quantity,
                    MovementThreshold = 0,
                    AlertDate = DateTime.UtcNow,
                    Severity = "Low"
                });
            }
        }

        return Ok(alerts.OrderByDescending(a => a.Severity == "Critical" ? 4 : 
                                               a.Severity == "High" ? 3 : 
                                               a.Severity == "Medium" ? 2 : 1));
    }

    /// <summary>
    /// Create a manual product movement (adjustment, transfer, etc.)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductMovementResponse>> CreateProductMovement(CreateProductMovementRequest request)
    {
        // Validate product exists
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return BadRequest($"Product with ID {request.ProductId} not found.");
        }

        // Validate warehouse exists
        var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
        if (warehouse == null)
        {
            return BadRequest($"Warehouse with ID {request.WarehouseId} not found.");
        }

        var currentUserId = GetCurrentUserId();

        var movement = new ProductMovement
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Direction = request.Direction,
            Description = request.Description,
            ReferenceNumber = request.ReferenceNumber,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            CreatedByUserId = currentUserId,
            MovementDate = request.MovementDate,
            Notes = request.Notes
        };

        _context.ProductMovements.Add(movement);
        await _context.SaveChangesAsync();

        // Update inventory if it's a real movement (not just a record)
        if (request.UpdateInventory)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == request.ProductId && pi.WarehouseId == request.WarehouseId);

            if (inventory == null)
            {
                inventory = new ProductInventory
                {
                    ProductId = request.ProductId,
                    WarehouseId = request.WarehouseId,
                    Quantity = request.Direction == "In" ? request.Quantity : -request.Quantity,
                    Unit = request.Unit
                };
                _context.ProductInventories.Add(inventory);
            }
            else
            {
                if (request.Direction == "In")
                    inventory.Quantity += request.Quantity;
                else
                    inventory.Quantity -= request.Quantity;
                
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync("ProductMovement", movement.Id.ToString(), "Created", 
            $"Created {request.MovementType} movement for {request.Quantity} {request.Unit}", null, currentUserId);

        return CreatedAtAction(nameof(GetProductMovement), new { id = movement.Id }, await GetProductMovement(movement.Id));
    }

    /// <summary>
    /// Get a specific product movement by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductMovementResponse>> GetProductMovement(int id)
    {
        var movement = await _context.ProductMovements
            .Include(pm => pm.Product)
            .Include(pm => pm.Warehouse)
            .Include(pm => pm.CreatedByUser)
            .FirstOrDefaultAsync(pm => pm.Id == id);

        if (movement == null)
        {
            return NotFound();
        }

        var response = new ProductMovementResponse
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = movement.Product.Name,
            ProductSKU = movement.Product.SKU ?? "",
            WarehouseId = movement.WarehouseId,
            WarehouseName = movement.Warehouse.Name,
            MovementType = movement.MovementType,
            Quantity = movement.Quantity,
            Unit = movement.Unit,
            Direction = movement.Direction,
            Description = movement.Description,
            ReferenceNumber = movement.ReferenceNumber,
            ReferenceId = movement.ReferenceId,
            ReferenceType = movement.ReferenceType,
            CreatedByUserName = movement.CreatedByUser?.FullName,
            MovementDate = movement.MovementDate,
            CreatedAt = movement.CreatedAt,
            Notes = movement.Notes
        };

        return Ok(response);
    }
}

// Additional DTO for creating movements
public class CreateProductMovementRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool UpdateInventory { get; set; } = true;
}
