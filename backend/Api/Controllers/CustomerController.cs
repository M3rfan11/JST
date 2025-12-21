using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Models;
using Api.Data;
using Api.Services;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class CustomerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: api/Customer/lookup/{phoneNumber}
        [HttpGet("lookup/{phoneNumber}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Customer>> LookupCustomer(string phoneNumber)
        {
            try
            {
                Console.WriteLine($"Looking up customer with phone: {phoneNumber}");
                
                // First, try to find customer in Customers table
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && c.IsActive);

                if (customer != null)
                {
                    Console.WriteLine($"Found customer in Customers table: {customer.FullName}");
                    return Ok(customer);
                }

                // If not found in Customers table, check existing sales orders
                // Prioritize orders with actual customer names over "Walk-in Customer"
                var existingSale = await _context.SalesOrders
                    .Where(s => s.CustomerPhone == phoneNumber)
                    .OrderByDescending(s => s.CustomerName != "Walk-in Customer")
                    .ThenByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingSale != null)
                {
                    Console.WriteLine($"Found customer in SalesOrders: {existingSale.CustomerName}");
                    // Create a customer object from the sales order data
                    var customerFromSale = new Customer
                    {
                        FullName = existingSale.CustomerName ?? "Unknown Customer",
                        PhoneNumber = existingSale.CustomerPhone,
                        Email = existingSale.CustomerEmail,
                        Address = existingSale.CustomerAddress,
                        CreatedAt = existingSale.CreatedAt,
                        IsActive = true
                    };

                    return Ok(customerFromSale);
                }

                Console.WriteLine($"Customer not found for phone: {phoneNumber}");
                return NotFound(new { message = "Customer not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error looking up customer: {ex.Message}");
                return StatusCode(500, new { message = "Error looking up customer", error = ex.Message });
            }
        }

        // POST: api/Customer/register
        [HttpPost("register")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Customer>> RegisterCustomer([FromBody] RegisterCustomerRequest request)
        {
            try
            {
                // Check if customer already exists with this phone number
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber);

                if (existingCustomer != null)
                {
                    return Conflict(new { message = "Customer with this phone number already exists" });
                }

                var customer = new Customer
                {
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Customer", customer.Id.ToString(), "Created", 
                    null, System.Text.Json.JsonSerializer.Serialize(customer), GetCurrentUserId());

                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error registering customer", error = ex.Message });
            }
        }

        // GET: api/Customer/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (customer == null)
                {
                    return NotFound();
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving customer", error = ex.Message });
            }
        }

        // GET: api/Customer
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.FullName)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving customers", error = ex.Message });
            }
        }

        // PUT: api/Customer/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound();
                }

                var beforeJson = System.Text.Json.JsonSerializer.Serialize(customer);

                customer.FullName = request.FullName;
                customer.PhoneNumber = request.PhoneNumber;
                customer.Email = request.Email;
                customer.Address = request.Address;
                customer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var afterJson = System.Text.Json.JsonSerializer.Serialize(customer);
                await _auditService.LogAsync("Customer", customer.Id.ToString(), "Updated", 
                    beforeJson, afterJson, GetCurrentUserId());

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating customer", error = ex.Message });
            }
        }
    }

    public class RegisterCustomerRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}
