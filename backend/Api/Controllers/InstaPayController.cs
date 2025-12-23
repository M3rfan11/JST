using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstaPayController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstaPayController> _logger;
    private readonly IWebHostEnvironment _environment;

    public InstaPayController(
        ApplicationDbContext context,
        ILogger<InstaPayController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get order details for payment page (allows anonymous for guest orders)
    /// </summary>
    [HttpGet("orders/{orderId}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetOrder(int orderId)
    {
        try
        {
            var order = await _context.SalesOrders
                .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
                .Include(so => so.PaymentProofs)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            // Check if order is InstaPay
            if (order.PaymentMethod?.ToUpper() != "INSTAPAY")
            {
                return BadRequest("This order is not an InstaPay order");
            }

            var response = new
            {
                id = order.Id,
                orderNumber = order.OrderNumber,
                totalAmount = order.TotalAmount,
                status = order.Status,
                paymentStatus = order.PaymentStatus,
                customerName = order.CustomerName,
                customerPhone = order.CustomerPhone,
                hasProof = order.PaymentProofs.Any(),
                latestProof = order.PaymentProofs
                    .OrderByDescending(p => p.UploadedAt)
                    .Select(p => new
                    {
                        id = p.Id,
                        fileUrl = p.FileUrl,
                        fileType = p.FileType,
                        fileName = p.FileName,
                        uploadedAt = p.UploadedAt
                    })
                    .FirstOrDefault()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving InstaPay order {OrderId}", orderId);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Upload payment proof (allows anonymous for guest orders)
    /// </summary>
    [HttpPost("orders/{orderId}/proof")]
    [AllowAnonymous]
    public async Task<ActionResult> UploadProof(int orderId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "application/pdf" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Invalid file type. Only JPG, PNG, and PDF files are allowed.");
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest("File size exceeds 5MB limit");
            }

            var order = await _context.SalesOrders
                .Include(so => so.PaymentProofs)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            // Check if order is InstaPay
            if (order.PaymentMethod?.ToUpper() != "INSTAPAY")
            {
                return BadRequest("This order is not an InstaPay order");
            }

            // Check if order is in valid status for proof upload
            if (order.Status != "PENDING_PAYMENT" && order.Status != "REJECTED")
            {
                return BadRequest($"Cannot upload proof for order with status: {order.Status}");
            }

            // Save file to wwwroot/uploads/payment-proofs for static file serving
            var wwwrootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(wwwrootPath, "uploads", "payment-proofs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var fileUrl = $"/uploads/payment-proofs/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create payment proof record
            var proof = new PaymentProof
            {
                OrderId = orderId,
                FileUrl = fileUrl,
                FileType = file.ContentType,
                FileName = file.FileName,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.PaymentProofs.Add(proof);

            // Update order status
            order.Status = "PROOF_SUBMITTED";
            order.UpdatedAt = DateTime.UtcNow;
            order.RejectionReason = null; // Clear rejection reason if re-uploading

            // Create tracking entry
            var tracking = new OrderTracking
            {
                OrderId = orderId,
                Status = "PROOF_SUBMITTED",
                Notes = "Payment proof uploaded",
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = GetCurrentUserId() > 0 ? GetCurrentUserId() : order.CreatedByUserId
            };
            _context.OrderTrackings.Add(tracking);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment proof uploaded successfully",
                proofId = proof.Id,
                fileUrl = proof.FileUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading payment proof for order {OrderId}", orderId);
            return StatusCode(500, new { message = "An error occurred while uploading the proof" });
        }
    }

    /// <summary>
    /// Get orders pending proof review (Admin only)
    /// </summary>
    [HttpGet("admin/pending-proofs")]
    [Authorize]
    public async Task<ActionResult> GetPendingProofs()
    {
        try
        {
            var orders = await _context.SalesOrders
                .Include(so => so.PaymentProofs.OrderByDescending(p => p.UploadedAt))
                .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
                .Where(so => so.PaymentMethod != null && 
                             so.PaymentMethod.ToUpper() == "INSTAPAY" &&
                             (so.Status == "PROOF_SUBMITTED" || so.Status == "UNDER_REVIEW"))
                .OrderByDescending(so => so.CreatedAt)
                .Select(so => new
                {
                    id = so.Id,
                    orderNumber = so.OrderNumber,
                    customerName = so.CustomerName,
                    customerPhone = so.CustomerPhone,
                    customerEmail = so.CustomerEmail,
                    totalAmount = so.TotalAmount,
                    status = so.Status,
                    createdAt = so.CreatedAt,
                    latestProof = so.PaymentProofs
                        .OrderByDescending(p => p.UploadedAt)
                        .Select(p => new
                        {
                            id = p.Id,
                            fileUrl = p.FileUrl,
                            fileType = p.FileType,
                            fileName = p.FileName,
                            uploadedAt = p.UploadedAt
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending proofs");
            return StatusCode(500, new { message = "An error occurred while retrieving pending proofs" });
        }
    }

    /// <summary>
    /// Accept payment proof (Admin only)
    /// </summary>
    [HttpPost("admin/orders/{orderId}/accept")]
    [Authorize]
    public async Task<ActionResult> AcceptPayment(int orderId, [FromBody] AcceptPaymentRequest? request = null)
    {
        try
        {
            var order = await _context.SalesOrders
                .Include(so => so.PaymentProofs)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            if (order.PaymentMethod?.ToUpper() != "INSTAPAY")
            {
                return BadRequest("This order is not an InstaPay order");
            }

            if (order.Status != "PROOF_SUBMITTED" && order.Status != "UNDER_REVIEW")
            {
                return BadRequest($"Cannot accept payment for order with status: {order.Status}");
            }

            var userId = GetCurrentUserId();

            // Update order
            order.Status = "ACCEPTED";
            order.PaymentStatus = "Paid";
            order.ConfirmedAt = DateTime.UtcNow;
            order.ConfirmedByUserId = userId;
            order.AdminNote = request?.AdminNote;
            order.UpdatedAt = DateTime.UtcNow;

            // Create tracking entry
            var tracking = new OrderTracking
            {
                OrderId = orderId,
                Status = "ACCEPTED",
                Notes = request?.AdminNote ?? "Payment confirmed by admin",
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.OrderTrackings.Add(tracking);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment accepted successfully",
                orderId = order.Id,
                status = order.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting payment for order {OrderId}", orderId);
            return StatusCode(500, new { message = "An error occurred while accepting the payment" });
        }
    }

    /// <summary>
    /// Reject payment proof (Admin only)
    /// </summary>
    [HttpPost("admin/orders/{orderId}/reject")]
    [Authorize]
    public async Task<ActionResult> RejectPayment(int orderId, [FromBody] RejectPaymentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return BadRequest("Rejection reason is required");
            }

            var order = await _context.SalesOrders
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            if (order.PaymentMethod?.ToUpper() != "INSTAPAY")
            {
                return BadRequest("This order is not an InstaPay order");
            }

            if (order.Status != "PROOF_SUBMITTED" && order.Status != "UNDER_REVIEW")
            {
                return BadRequest($"Cannot reject payment for order with status: {order.Status}");
            }

            var userId = GetCurrentUserId();

            // Update order
            order.Status = "REJECTED";
            order.RejectionReason = request.RejectionReason;
            order.AdminNote = request.AdminNote;
            order.UpdatedAt = DateTime.UtcNow;

            // Create tracking entry
            var tracking = new OrderTracking
            {
                OrderId = orderId,
                Status = "REJECTED",
                Notes = $"Payment rejected: {request.RejectionReason}",
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = userId
            };
            _context.OrderTrackings.Add(tracking);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment rejected",
                orderId = order.Id,
                status = order.Status,
                rejectionReason = order.RejectionReason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting payment for order {OrderId}", orderId);
            return StatusCode(500, new { message = "An error occurred while rejecting the payment" });
        }
    }
}

// DTOs for InstaPay endpoints
public class AcceptPaymentRequest
{
    public string? AdminNote { get; set; }
}

public class RejectPaymentRequest
{
    [Required]
    public string RejectionReason { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}

