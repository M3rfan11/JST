using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromoCodeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PromoCodeController> _logger;

    public PromoCodeController(
        ApplicationDbContext context,
        IAuditService auditService,
        IEmailService emailService,
        ILogger<PromoCodeController> logger)
    {
        _context = context;
        _auditService = auditService;
        _emailService = emailService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get all promo codes
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<PromoCodeListResponse>>> GetPromoCodes()
    {
        try
        {
            var promoCodes = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                .Include(pc => pc.PromoCodeProducts)
                .Include(pc => pc.CreatedByUser)
                .OrderByDescending(pc => pc.CreatedAt)
                .ToListAsync();

            var response = promoCodes.Select(pc =>
            {
                var now = DateTime.UtcNow;
                var isExpired = pc.EndDate.HasValue && pc.EndDate.Value < now;
                var isValid = pc.IsActive && !isExpired && 
                              (pc.UsageLimit == null || pc.UsedCount < pc.UsageLimit.Value);

                return new PromoCodeListResponse
                {
                    Id = pc.Id,
                    Code = pc.Code,
                    Description = pc.Description,
                    DiscountType = pc.DiscountType,
                    DiscountValue = pc.DiscountValue,
                    StartDate = pc.StartDate,
                    EndDate = pc.EndDate,
                    UsageLimit = pc.UsageLimit,
                    UsedCount = pc.UsedCount,
                    UserCount = pc.PromoCodeUsers.Count,
                    ProductCount = pc.PromoCodeProducts.Count,
                    IsActive = pc.IsActive,
                    IsExpired = isExpired,
                    IsValid = isValid
                };
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promo codes");
            return StatusCode(500, new { message = "An error occurred while retrieving promo codes" });
        }
    }

    /// <summary>
    /// Get a specific promo code by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PromoCodeResponse>> GetPromoCode(int id)
    {
        try
        {
            var promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeProducts)
                    .ThenInclude(pcp => pcp.Product)
                .Include(pc => pc.CreatedByUser)
                .Include(pc => pc.PromoCodeUsages)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeUsages)
                    .ThenInclude(pcu => pcu.SalesOrder)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (promoCode == null)
            {
                return NotFound(new { message = "Promo code not found" });
            }

            var now = DateTime.UtcNow;
            var isExpired = promoCode.EndDate.HasValue && promoCode.EndDate.Value < now;
            var isValid = promoCode.IsActive && !isExpired && 
                          (promoCode.UsageLimit == null || promoCode.UsedCount < promoCode.UsageLimit.Value);

            // Get usage per user
            var userUsages = new List<UserUsageInfo>();
            if (promoCode.PromoCodeUsers.Any())
            {
                foreach (var promoCodeUser in promoCode.PromoCodeUsers)
                {
                    var userUsageRecords = promoCode.PromoCodeUsages
                        .Where(pcu => pcu.UserId == promoCodeUser.UserId)
                        .OrderByDescending(pcu => pcu.UsedAt)
                        .ToList();

                    var usageCount = userUsageRecords.Count;
                    var hasExceededLimit = promoCode.UsageLimitPerUser.HasValue && 
                                          usageCount >= promoCode.UsageLimitPerUser.Value;

                    userUsages.Add(new UserUsageInfo
                    {
                        UserId = promoCodeUser.UserId,
                        UserName = promoCodeUser.User?.FullName ?? "Unknown",
                        UserEmail = promoCodeUser.User?.Email,
                        UsageCount = usageCount,
                        UsageLimit = promoCode.UsageLimitPerUser,
                        HasExceededLimit = hasExceededLimit,
                        UsageRecords = userUsageRecords.Select(pcu => new UsageRecord
                        {
                            OrderId = pcu.SalesOrderId,
                            OrderNumber = pcu.SalesOrder?.OrderNumber ?? "N/A",
                            DiscountAmount = pcu.DiscountAmount,
                            UsedAt = pcu.UsedAt
                        }).ToList()
                    });
                }
            }
            else
            {
                // If no specific users assigned, show all usages grouped by user
                var allUsages = promoCode.PromoCodeUsages
                    .GroupBy(pcu => pcu.UserId)
                    .ToList();

                foreach (var usageGroup in allUsages)
                {
                    var userId = usageGroup.Key;
                    var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
                    var userUsageRecords = usageGroup.OrderByDescending(pcu => pcu.UsedAt).ToList();
                    var usageCount = userUsageRecords.Count;
                    var hasExceededLimit = promoCode.UsageLimitPerUser.HasValue && 
                                          usageCount >= promoCode.UsageLimitPerUser.Value;

                    userUsages.Add(new UserUsageInfo
                    {
                        UserId = userId ?? 0,
                        UserName = user?.FullName ?? (userId.HasValue ? "Unknown User" : "Guest"),
                        UserEmail = user?.Email,
                        UsageCount = usageCount,
                        UsageLimit = promoCode.UsageLimitPerUser,
                        HasExceededLimit = hasExceededLimit,
                        UsageRecords = userUsageRecords.Select(pcu => new UsageRecord
                        {
                            OrderId = pcu.SalesOrderId,
                            OrderNumber = pcu.SalesOrder?.OrderNumber ?? "N/A",
                            DiscountAmount = pcu.DiscountAmount,
                            UsedAt = pcu.UsedAt
                        }).ToList()
                    });
                }
            }

            var response = new PromoCodeResponse
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                Description = promoCode.Description,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue,
                StartDate = promoCode.StartDate,
                EndDate = promoCode.EndDate,
                UsageLimit = promoCode.UsageLimit,
                UsedCount = promoCode.UsedCount,
                UsageLimitPerUser = promoCode.UsageLimitPerUser,
                MinimumOrderAmount = promoCode.MinimumOrderAmount,
                MaximumDiscountAmount = promoCode.MaximumDiscountAmount,
                IsActive = promoCode.IsActive,
                CreatedAt = promoCode.CreatedAt,
                UpdatedAt = promoCode.UpdatedAt,
                CreatedByUserId = promoCode.CreatedByUserId,
                CreatedByUserName = promoCode.CreatedByUser?.FullName,
                UserIds = promoCode.PromoCodeUsers.Select(pcu => pcu.UserId).ToList(),
                UserNames = promoCode.PromoCodeUsers.Select(pcu => pcu.User?.FullName ?? "Unknown").ToList(),
                ProductIds = promoCode.PromoCodeProducts.Select(pcp => pcp.ProductId).ToList(),
                ProductNames = promoCode.PromoCodeProducts.Select(pcp => pcp.Product.Name).ToList(),
                IsExpired = isExpired,
                IsValid = isValid,
                UserUsages = userUsages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promo code {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the promo code" });
        }
    }

    /// <summary>
    /// Create a new promo code
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PromoCodeResponse>> CreatePromoCode([FromBody] CreatePromoCodeRequest request)
    {
        try
        {
            // Validate code uniqueness
            var existingCode = await _context.PromoCodes
                .FirstOrDefaultAsync(pc => pc.Code.ToUpper() == request.Code.ToUpper());
            
            if (existingCode != null)
            {
                return BadRequest(new { message = $"A promo code with code '{request.Code}' already exists" });
            }

            // Validate discount value
            if (request.DiscountType == "Percentage" && (request.DiscountValue < 0 || request.DiscountValue > 100))
            {
                return BadRequest(new { message = "Percentage discount must be between 0 and 100" });
            }

            if (request.DiscountValue < 0)
            {
                return BadRequest(new { message = "Discount value cannot be negative" });
            }

            // Validate dates
            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            var currentUserId = GetCurrentUserId();

            var promoCode = new PromoCode
            {
                Code = request.Code.ToUpper().Trim(),
                Description = request.Description?.Trim(),
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                UsageLimit = request.UsageLimit,
                UsageLimitPerUser = request.UsageLimitPerUser,
                MinimumOrderAmount = request.MinimumOrderAmount,
                MaximumDiscountAmount = request.MaximumDiscountAmount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            };

            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();

            // Assign users (only registered users can be assigned)
            if (request.UserIds != null && request.UserIds.Any())
            {
                // Filter out any invalid user IDs (0 or negative)
                var validUserIds = request.UserIds.Where(id => id > 0).ToList();
                
                if (validUserIds.Any())
                {
                    var users = await _context.Users
                        .Where(u => validUserIds.Contains(u.Id) && u.IsActive)
                        .ToListAsync();

                    var promoCodeUsers = users.Select(user => new PromoCodeUser
                    {
                        PromoCodeId = promoCode.Id,
                        UserId = user.Id,
                        AssignedAt = DateTime.UtcNow,
                        IsNotified = false
                    }).ToList();

                    _context.PromoCodeUsers.AddRange(promoCodeUsers);
                    await _context.SaveChangesAsync();

                    // Automatically send email notifications to all assigned registered users
                    foreach (var promoCodeUser in promoCodeUsers)
                    {
                        var user = users.FirstOrDefault(u => u.Id == promoCodeUser.UserId);
                        if (user == null) continue;
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            var emailSent = await _emailService.SendPromoCodeNotificationAsync(
                                user.Email,
                                user.FullName,
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );

                            if (emailSent)
                            {
                                promoCodeUser.IsNotified = true;
                                promoCodeUser.NotifiedAt = DateTime.UtcNow;
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Process email addresses: assign to registered users and send notifications
            if (request.EmailAddresses != null && request.EmailAddresses.Any())
            {
                var processedEmails = new HashSet<string>();
                
                foreach (var emailAddress in request.EmailAddresses)
                {
                    if (string.IsNullOrWhiteSpace(emailAddress)) continue;
                    
                    // Validate email format
                    var trimmedEmail = emailAddress.Trim().ToLower();
                    if (!trimmedEmail.Contains("@") || processedEmails.Contains(trimmedEmail)) continue;
                    processedEmails.Add(trimmedEmail);

                    // Check if this email belongs to a registered user
                    var registeredUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == trimmedEmail && u.IsActive);
                    
                    if (registeredUser != null)
                    {
                        // User is registered - add to PromoCodeUsers if not already added
                        var existingPromoCodeUser = await _context.PromoCodeUsers
                            .FirstOrDefaultAsync(pcu => pcu.PromoCodeId == promoCode.Id && pcu.UserId == registeredUser.Id);
                        
                        if (existingPromoCodeUser == null)
                        {
                            var promoCodeUser = new PromoCodeUser
                            {
                                PromoCodeId = promoCode.Id,
                                UserId = registeredUser.Id,
                                AssignedAt = DateTime.UtcNow,
                                IsNotified = false
                            };
                            _context.PromoCodeUsers.Add(promoCodeUser);
                            await _context.SaveChangesAsync();
                            
                            // Automatically send email notification to registered user
                            var emailSent = await _emailService.SendPromoCodeNotificationAsync(
                                registeredUser.Email!,
                                registeredUser.FullName,
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );

                            if (emailSent)
                            {
                                promoCodeUser.IsNotified = true;
                                promoCodeUser.NotifiedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    else
                    {
                        // Non-registered email - send notification
                        // Note: The email already includes a note that registration is required to use the code
                        if (request.SendEmailNotification)
                        {
                            await _emailService.SendPromoCodeNotificationAsync(
                                trimmedEmail,
                                "Valued Customer", // Generic name for non-registered
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );
                        }
                    }
                }
            }

            // Assign products (if specified, empty list means all products)
            if (request.ProductIds != null && request.ProductIds.Any())
            {
                var products = await _context.Products
                    .Where(p => request.ProductIds.Contains(p.Id))
                    .ToListAsync();

                var promoCodeProducts = products.Select(product => new PromoCodeProduct
                {
                    PromoCodeId = promoCode.Id,
                    ProductId = product.Id,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.PromoCodeProducts.AddRange(promoCodeProducts);
                await _context.SaveChangesAsync();
            }

            // Reload with all relationships
            promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeProducts)
                    .ThenInclude(pcp => pcp.Product)
                .Include(pc => pc.CreatedByUser)
                .FirstOrDefaultAsync(pc => pc.Id == promoCode.Id);

            var now = DateTime.UtcNow;
            var isExpired = promoCode.EndDate.HasValue && promoCode.EndDate.Value < now;
            var isValid = promoCode.IsActive && !isExpired && 
                          (promoCode.UsageLimit == null || promoCode.UsedCount < promoCode.UsageLimit.Value);

            var response = new PromoCodeResponse
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                Description = promoCode.Description,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue,
                StartDate = promoCode.StartDate,
                EndDate = promoCode.EndDate,
                UsageLimit = promoCode.UsageLimit,
                UsedCount = promoCode.UsedCount,
                UsageLimitPerUser = promoCode.UsageLimitPerUser,
                MinimumOrderAmount = promoCode.MinimumOrderAmount,
                MaximumDiscountAmount = promoCode.MaximumDiscountAmount,
                IsActive = promoCode.IsActive,
                CreatedAt = promoCode.CreatedAt,
                UpdatedAt = promoCode.UpdatedAt,
                CreatedByUserId = promoCode.CreatedByUserId,
                CreatedByUserName = promoCode.CreatedByUser?.FullName,
                UserIds = promoCode.PromoCodeUsers.Select(pcu => pcu.UserId).ToList(),
                UserNames = promoCode.PromoCodeUsers.Select(pcu => pcu.User?.FullName ?? "Unknown").ToList(),
                ProductIds = promoCode.PromoCodeProducts.Select(pcp => pcp.ProductId).ToList(),
                ProductNames = promoCode.PromoCodeProducts.Select(pcp => pcp.Product.Name).ToList(),
                IsExpired = isExpired,
                IsValid = isValid
            };

            await _auditService.LogAsync("PromoCode", promoCode.Id.ToString(), "Created", 
                $"Created promo code {promoCode.Code}", null, currentUserId);

            return CreatedAtAction(nameof(GetPromoCode), new { id = promoCode.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promo code");
            return StatusCode(500, new { message = "An error occurred while creating the promo code", error = ex.Message });
        }
    }

    /// <summary>
    /// Update a promo code
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PromoCodeResponse>> UpdatePromoCode(int id, [FromBody] UpdatePromoCodeRequest request)
    {
        try
        {
            var promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                .Include(pc => pc.PromoCodeProducts)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (promoCode == null)
            {
                return NotFound(new { message = "Promo code not found" });
            }

            // Update code if provided
            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code.ToUpper() != promoCode.Code.ToUpper())
            {
                var existingCode = await _context.PromoCodes
                    .FirstOrDefaultAsync(pc => pc.Code.ToUpper() == request.Code.ToUpper() && pc.Id != id);
                
                if (existingCode != null)
                {
                    return BadRequest(new { message = $"A promo code with code '{request.Code}' already exists" });
                }
                promoCode.Code = request.Code.ToUpper().Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
                promoCode.Description = request.Description.Trim();

            if (!string.IsNullOrWhiteSpace(request.DiscountType))
                promoCode.DiscountType = request.DiscountType;

            if (request.DiscountValue.HasValue)
            {
                if (promoCode.DiscountType == "Percentage" && (request.DiscountValue.Value < 0 || request.DiscountValue.Value > 100))
                {
                    return BadRequest(new { message = "Percentage discount must be between 0 and 100" });
                }
                promoCode.DiscountValue = request.DiscountValue.Value;
            }

            if (request.StartDate.HasValue)
                promoCode.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
            {
                var startDate = request.StartDate ?? promoCode.StartDate;
                if (request.EndDate.Value < startDate)
                {
                    return BadRequest(new { message = "End date must be after start date" });
                }
                promoCode.EndDate = request.EndDate.Value;
            }

            if (request.UsageLimit.HasValue)
                promoCode.UsageLimit = request.UsageLimit.Value;

            if (request.UsageLimitPerUser.HasValue)
                promoCode.UsageLimitPerUser = request.UsageLimitPerUser.Value;

            if (request.MinimumOrderAmount.HasValue)
                promoCode.MinimumOrderAmount = request.MinimumOrderAmount.Value;

            if (request.MaximumDiscountAmount.HasValue)
                promoCode.MaximumDiscountAmount = request.MaximumDiscountAmount.Value;

            if (request.IsActive.HasValue)
                promoCode.IsActive = request.IsActive.Value;

            promoCode.UpdatedAt = DateTime.UtcNow;

            // Update users if provided (only registered users can be assigned)
            if (request.UserIds != null)
            {
                // Get existing user IDs to identify newly added users
                var existingUserIds = promoCode.PromoCodeUsers.Select(pcu => pcu.UserId).ToList();
                
                // Remove existing user associations
                _context.PromoCodeUsers.RemoveRange(promoCode.PromoCodeUsers);

                // Filter out any invalid user IDs (0 or negative)
                var validUserIds = request.UserIds.Where(id => id > 0).ToList();

                if (validUserIds.Any())
                {
                    var users = await _context.Users
                        .Where(u => validUserIds.Contains(u.Id) && u.IsActive)
                        .ToListAsync();

                    var promoCodeUsers = users.Select(user => new PromoCodeUser
                    {
                        PromoCodeId = promoCode.Id,
                        UserId = user.Id,
                        AssignedAt = DateTime.UtcNow,
                        IsNotified = existingUserIds.Contains(user.Id) // Keep notification status for existing users
                    }).ToList();

                    _context.PromoCodeUsers.AddRange(promoCodeUsers);
                    await _context.SaveChangesAsync();

                    // Automatically send email notifications to newly added users
                    var newlyAddedUsers = promoCodeUsers.Where(pcu => !existingUserIds.Contains(pcu.UserId)).ToList();
                    foreach (var promoCodeUser in newlyAddedUsers)
                    {
                        var user = users.FirstOrDefault(u => u.Id == promoCodeUser.UserId);
                        if (user == null) continue;
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            var emailSent = await _emailService.SendPromoCodeNotificationAsync(
                                user.Email,
                                user.FullName,
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );

                            if (emailSent)
                            {
                                promoCodeUser.IsNotified = true;
                                promoCodeUser.NotifiedAt = DateTime.UtcNow;
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Process email addresses: assign to registered users and send notifications
            if (request.EmailAddresses != null && request.EmailAddresses.Any())
            {
                var processedEmails = new HashSet<string>();
                var existingUserIds = promoCode.PromoCodeUsers.Select(pcu => pcu.UserId).ToList();
                
                foreach (var emailAddress in request.EmailAddresses)
                {
                    if (string.IsNullOrWhiteSpace(emailAddress)) continue;
                    
                    // Validate email format
                    var trimmedEmail = emailAddress.Trim().ToLower();
                    if (!trimmedEmail.Contains("@") || processedEmails.Contains(trimmedEmail)) continue;
                    processedEmails.Add(trimmedEmail);

                    // Check if this email belongs to a registered user
                    var registeredUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == trimmedEmail && u.IsActive);
                    
                    if (registeredUser != null)
                    {
                        // User is registered - add to PromoCodeUsers if not already added
                        var existingPromoCodeUser = await _context.PromoCodeUsers
                            .FirstOrDefaultAsync(pcu => pcu.PromoCodeId == promoCode.Id && pcu.UserId == registeredUser.Id);
                        
                        if (existingPromoCodeUser == null)
                        {
                            var promoCodeUser = new PromoCodeUser
                            {
                                PromoCodeId = promoCode.Id,
                                UserId = registeredUser.Id,
                                AssignedAt = DateTime.UtcNow,
                                IsNotified = false
                            };
                            _context.PromoCodeUsers.Add(promoCodeUser);
                            await _context.SaveChangesAsync();
                            
                            // Automatically send email notification to newly added registered user
                            var emailSent = await _emailService.SendPromoCodeNotificationAsync(
                                registeredUser.Email!,
                                registeredUser.FullName,
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );

                            if (emailSent)
                            {
                                promoCodeUser.IsNotified = true;
                                promoCodeUser.NotifiedAt = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    else
                    {
                        // Non-registered email - send notification if SendEmailNotification is enabled (defaults to true)
                        if (request.SendEmailNotification ?? true)
                        {
                            await _emailService.SendPromoCodeNotificationAsync(
                                trimmedEmail,
                                "Valued Customer",
                                promoCode.Code,
                                promoCode.DiscountValue,
                                promoCode.DiscountType,
                                promoCode.EndDate
                            );
                        }
                    }
                }
            }

            // Update products if provided
            if (request.ProductIds != null)
            {
                // Remove existing product associations
                _context.PromoCodeProducts.RemoveRange(promoCode.PromoCodeProducts);

                if (request.ProductIds.Any())
                {
                    var products = await _context.Products
                        .Where(p => request.ProductIds.Contains(p.Id))
                        .ToListAsync();

                    var promoCodeProducts = products.Select(product => new PromoCodeProduct
                    {
                        PromoCodeId = promoCode.Id,
                        ProductId = product.Id,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.PromoCodeProducts.AddRange(promoCodeProducts);
                }
            }

            await _context.SaveChangesAsync();

            // Reload with all relationships
            promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeProducts)
                    .ThenInclude(pcp => pcp.Product)
                .Include(pc => pc.CreatedByUser)
                .FirstOrDefaultAsync(pc => pc.Id == promoCode.Id);

            var now = DateTime.UtcNow;
            var isExpired = promoCode.EndDate.HasValue && promoCode.EndDate.Value < now;
            var isValid = promoCode.IsActive && !isExpired && 
                          (promoCode.UsageLimit == null || promoCode.UsedCount < promoCode.UsageLimit.Value);

            var response = new PromoCodeResponse
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                Description = promoCode.Description,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue,
                StartDate = promoCode.StartDate,
                EndDate = promoCode.EndDate,
                UsageLimit = promoCode.UsageLimit,
                UsedCount = promoCode.UsedCount,
                UsageLimitPerUser = promoCode.UsageLimitPerUser,
                MinimumOrderAmount = promoCode.MinimumOrderAmount,
                MaximumDiscountAmount = promoCode.MaximumDiscountAmount,
                IsActive = promoCode.IsActive,
                CreatedAt = promoCode.CreatedAt,
                UpdatedAt = promoCode.UpdatedAt,
                CreatedByUserId = promoCode.CreatedByUserId,
                CreatedByUserName = promoCode.CreatedByUser?.FullName,
                UserIds = promoCode.PromoCodeUsers.Select(pcu => pcu.UserId).ToList(),
                UserNames = promoCode.PromoCodeUsers.Select(pcu => pcu.User?.FullName ?? "Unknown").ToList(),
                ProductIds = promoCode.PromoCodeProducts.Select(pcp => pcp.ProductId).ToList(),
                ProductNames = promoCode.PromoCodeProducts.Select(pcp => pcp.Product.Name).ToList(),
                IsExpired = isExpired,
                IsValid = isValid
            };

            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync("PromoCode", promoCode.Id.ToString(), "Updated", 
                $"Updated promo code {promoCode.Code}", null, currentUserId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promo code {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the promo code", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a promo code (soft delete - sets IsActive to false)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeletePromoCode(int id)
    {
        try
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
            {
                return NotFound(new { message = "Promo code not found" });
            }

            // Soft delete - set IsActive to false (this expires it for users)
            promoCode.IsActive = false;
            promoCode.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync("PromoCode", promoCode.Id.ToString(), "Deleted", 
                $"Deleted (deactivated) promo code {promoCode.Code}", null, currentUserId);

            return Ok(new { message = "Promo code deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promo code {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the promo code", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all customers (registered users and order customers) for promo code assignment
    /// </summary>
    [HttpGet("customers")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<PromoCodeCustomerResponse>>> GetCustomersForPromoCode()
    {
        try
        {
            var customers = new List<PromoCodeCustomerResponse>();

            // Get all registered users (who have signed up)
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                var registeredUsers = await _context.Users
                    .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.RoleId == customerRole.Id))
                    .ToListAsync();

                foreach (var user in registeredUsers)
                {
                    var userOrders = await _context.SalesOrders
                        .Where(so => so.CustomerEmail != null && so.CustomerEmail.ToLower() == user.Email.ToLower())
                        .ToListAsync();

                    customers.Add(new PromoCodeCustomerResponse
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Phone = null,
                        IsRegistered = true,
                        OrderCount = userOrders.Count,
                        LastOrderDate = userOrders.Any() ? userOrders.Max(so => so.OrderDate) : (DateTime?)null
                    });
                }
            }

            // Get unique customers from orders (who are not registered users)
            var registeredEmails = customers.Select(c => c.Email?.ToLower()).Where(e => !string.IsNullOrEmpty(e)).ToList();
            
            var orderCustomersByEmail = await _context.SalesOrders
                .Where(so => !string.IsNullOrEmpty(so.CustomerEmail) && 
                            !registeredEmails.Contains(so.CustomerEmail!.ToLower()))
                .GroupBy(so => so.CustomerEmail!.ToLower())
                .Select(g => new PromoCodeCustomerResponse
                {
                    Id = 0, // No user ID for non-registered customers
                    Email = g.Key,
                    FullName = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Unknown Customer",
                    Phone = g.OrderByDescending(so => so.CreatedAt).First().CustomerPhone,
                    IsRegistered = false,
                    OrderCount = g.Count(),
                    LastOrderDate = g.Max(so => so.OrderDate)
                })
                .ToListAsync();

            customers.AddRange(orderCustomersByEmail);

            // Get customers by phone (for those without email and not registered)
            var registeredPhones = customers.Where(c => c.Phone != null).Select(c => c.Phone).ToList();
            
            var orderCustomersByPhone = await _context.SalesOrders
                .Where(so => string.IsNullOrEmpty(so.CustomerEmail) && 
                            !string.IsNullOrEmpty(so.CustomerPhone) &&
                            !registeredPhones.Contains(so.CustomerPhone))
                .GroupBy(so => so.CustomerPhone!)
                .Select(g => new PromoCodeCustomerResponse
                {
                    Id = 0, // No user ID for non-registered customers
                    Email = null,
                    FullName = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Unknown Customer",
                    Phone = g.Key,
                    IsRegistered = false,
                    OrderCount = g.Count(),
                    LastOrderDate = g.Max(so => so.OrderDate)
                })
                .ToListAsync();

            customers.AddRange(orderCustomersByPhone);

            // Sort by last order date (most recent first), then by name
            var sortedCustomers = customers
                .OrderByDescending(c => c.LastOrderDate ?? DateTime.MinValue)
                .ThenBy(c => c.FullName)
                .ToList();

            return Ok(sortedCustomers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers for promo code");
            return StatusCode(500, new { message = "An error occurred while retrieving customers" });
        }
    }

    /// <summary>
    /// Validate a promo code for a user
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidatePromoCode([FromBody] ValidatePromoCodeRequest request)
    {
        try
        {
            var promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                .Include(pc => pc.PromoCodeProducts)
                .FirstOrDefaultAsync(pc => pc.Code.ToUpper() == request.Code.ToUpper());

            if (promoCode == null)
            {
                return BadRequest(new { valid = false, message = "Invalid promo code" });
            }

            var now = DateTime.UtcNow;

            // Check if active
            if (!promoCode.IsActive)
            {
                return Ok(new { valid = false, message = "This promo code has been deactivated" });
            }

            // Check date validity
            if (now < promoCode.StartDate)
            {
                return Ok(new { valid = false, message = "This promo code is not yet valid" });
            }

            if (promoCode.EndDate.HasValue && now > promoCode.EndDate.Value)
            {
                return Ok(new { valid = false, message = "This promo code has expired" });
            }

            // Check usage limit
            if (promoCode.UsageLimit.HasValue && promoCode.UsedCount >= promoCode.UsageLimit.Value)
            {
                return Ok(new { valid = false, message = "This promo code has reached its usage limit" });
            }

            // Check user assignment (if users are specified, user must be in the list)
            // IMPORTANT: Only registered users (with UserId) can use promo codes
            if (promoCode.PromoCodeUsers.Any())
            {
                if (request.UserId == null)
                {
                    return Ok(new { valid = false, message = "You must be a registered user to use this promo code. Please sign up first." });
                }

                if (!promoCode.PromoCodeUsers.Any(pcu => pcu.UserId == request.UserId))
                {
                    return Ok(new { valid = false, message = "This promo code is not available for your account" });
                }

                // Check per-user usage limit
                if (promoCode.UsageLimitPerUser.HasValue)
                {
                    var userUsageCount = await _context.PromoCodeUsages
                        .CountAsync(pcu => pcu.PromoCodeId == promoCode.Id && pcu.UserId == request.UserId);
                    
                    if (userUsageCount >= promoCode.UsageLimitPerUser.Value)
                    {
                        return Ok(new { valid = false, message = "You have reached the usage limit for this promo code" });
                    }
                }
            }

            // Check minimum order amount
            if (promoCode.MinimumOrderAmount.HasValue && request.OrderAmount < promoCode.MinimumOrderAmount.Value)
            {
                return Ok(new { 
                    valid = false, 
                    message = $"Minimum order amount of LE {promoCode.MinimumOrderAmount.Value:F2} required" 
                });
            }

            // Check if applicable to specific products
            if (promoCode.PromoCodeProducts.Any())
            {
                if (request.ProductIds == null || !request.ProductIds.Any())
                {
                    return Ok(new { valid = false, message = "This promo code requires specific products in your cart." });
                }
                if (!request.ProductIds.Any(id => promoCode.PromoCodeProducts.Any(pcp => pcp.ProductId == id)))
                {
                    return Ok(new { valid = false, message = "None of the products in your cart are eligible for this promo code." });
                }
            }

            // Calculate discount
            decimal discountAmount = 0;
            if (promoCode.DiscountType == "Percentage")
            {
                discountAmount = request.OrderAmount * (promoCode.DiscountValue / 100);
            }
            else
            {
                discountAmount = promoCode.DiscountValue;
            }

            // Apply maximum discount limit
            if (promoCode.MaximumDiscountAmount.HasValue && discountAmount > promoCode.MaximumDiscountAmount.Value)
            {
                discountAmount = promoCode.MaximumDiscountAmount.Value;
            }

            // Check if products are specified
            var appliesToProducts = promoCode.PromoCodeProducts.Any();
            var productIds = request.ProductIds ?? new List<int>();

            return Ok(new
            {
                valid = true,
                promoCodeId = promoCode.Id,
                code = promoCode.Code,
                discountType = promoCode.DiscountType,
                discountValue = promoCode.DiscountValue,
                discountAmount = discountAmount,
                appliesToProducts = appliesToProducts,
                productIds = appliesToProducts ? promoCode.PromoCodeProducts.Select(pcp => pcp.ProductId).ToList() : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code");
            return StatusCode(500, new { message = "An error occurred while validating the promo code" });
        }
    }

    /// <summary>
    /// Get usage details for a promo code (per-user tracking)
    /// </summary>
    [HttpGet("{id}/usage")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PromoCodeUsageDetailResponse>> GetPromoCodeUsage(int id)
    {
        try
        {
            var promoCode = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsers)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeUsages)
                    .ThenInclude(pcu => pcu.User)
                .Include(pc => pc.PromoCodeUsages)
                    .ThenInclude(pcu => pcu.SalesOrder)
                .FirstOrDefaultAsync(pc => pc.Id == id);

            if (promoCode == null)
            {
                return NotFound(new { message = "Promo code not found" });
            }

            var userUsages = new List<UserUsageInfo>();

            // If specific users are assigned, show their usage
            if (promoCode.PromoCodeUsers.Any())
            {
                foreach (var promoCodeUser in promoCode.PromoCodeUsers)
                {
                    var userUsageRecords = promoCode.PromoCodeUsages
                        .Where(pcu => pcu.UserId == promoCodeUser.UserId)
                        .OrderByDescending(pcu => pcu.UsedAt)
                        .ToList();

                    var usageCount = userUsageRecords.Count;
                    var hasExceededLimit = promoCode.UsageLimitPerUser.HasValue && 
                                          usageCount >= promoCode.UsageLimitPerUser.Value;

                    userUsages.Add(new UserUsageInfo
                    {
                        UserId = promoCodeUser.UserId,
                        UserName = promoCodeUser.User?.FullName ?? "Unknown",
                        UserEmail = promoCodeUser.User?.Email,
                        UsageCount = usageCount,
                        UsageLimit = promoCode.UsageLimitPerUser,
                        HasExceededLimit = hasExceededLimit,
                        UsageRecords = userUsageRecords.Select(pcu => new UsageRecord
                        {
                            OrderId = pcu.SalesOrderId,
                            OrderNumber = pcu.SalesOrder?.OrderNumber ?? "N/A",
                            DiscountAmount = pcu.DiscountAmount,
                            UsedAt = pcu.UsedAt
                        }).ToList()
                    });
                }
            }
            else
            {
                // If no specific users assigned, show all usages grouped by user
                var allUsages = promoCode.PromoCodeUsages
                    .GroupBy(pcu => pcu.UserId)
                    .ToList();

                foreach (var usageGroup in allUsages)
                {
                    var userId = usageGroup.Key;
                    var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
                    var userUsageRecords = usageGroup.OrderByDescending(pcu => pcu.UsedAt).ToList();
                    var usageCount = userUsageRecords.Count;
                    var hasExceededLimit = promoCode.UsageLimitPerUser.HasValue && 
                                          usageCount >= promoCode.UsageLimitPerUser.Value;

                    userUsages.Add(new UserUsageInfo
                    {
                        UserId = userId ?? 0,
                        UserName = user?.FullName ?? (userId.HasValue ? "Unknown User" : "Guest"),
                        UserEmail = user?.Email,
                        UsageCount = usageCount,
                        UsageLimit = promoCode.UsageLimitPerUser,
                        HasExceededLimit = hasExceededLimit,
                        UsageRecords = userUsageRecords.Select(pcu => new UsageRecord
                        {
                            OrderId = pcu.SalesOrderId,
                            OrderNumber = pcu.SalesOrder?.OrderNumber ?? "N/A",
                            DiscountAmount = pcu.DiscountAmount,
                            UsedAt = pcu.UsedAt
                        }).ToList()
                    });
                }
            }

            var response = new PromoCodeUsageDetailResponse
            {
                PromoCodeId = promoCode.Id,
                Code = promoCode.Code,
                UserUsages = userUsages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promo code usage for {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving promo code usage" });
        }
    }
}

public class ValidatePromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public decimal OrderAmount { get; set; }
    public List<int>? ProductIds { get; set; }
}

public class PromoCodeCustomerResponse
{
    public int Id { get; set; } // UserId if registered, 0 if not registered
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsRegistered { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

