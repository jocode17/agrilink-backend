using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AgriLinkDbContext _db;

    public AdminController(AgriLinkDbContext db)
    {
        _db = db;
    }

    // ── Product Management ───────────────────────────────────

    [HttpGet("products")]
    public async Task<ActionResult> GetAllProducts()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Farm)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new {
                p.Id,
                p.Name,
                p.Description,
                p.PricePerUnit,
                p.Unit,
                p.StockQuantity,
                p.Capacity,
                p.Status,
                p.LowStockThreshold,
                p.IsActive,
                p.SeedingDate,
                p.HarvestDate,
                CategoryName = p.Category.Name,
                FarmName = p.Farm.FarmName,
                p.CreatedAt
            })
            .ToListAsync();
        return Ok(products);
    }

    [HttpPost("products")]
    public async Task<ActionResult> CreateProduct([FromBody] AdminCreateProductRequest request)
    {
        try
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                FarmId = request.FarmId,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Description = request.Description,
                PricePerUnit = request.PricePerUnit,
                Unit = request.Unit ?? "kg",
                StockQuantity = request.StockQuantity,
                Capacity = request.Capacity,
                LowStockThreshold = request.LowStockThreshold,
                Status = request.StockQuantity > request.LowStockThreshold ? "available" :
                         request.StockQuantity > 0 ? "low_stock" : "out_of_stock",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Product created", productId = product.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("products/{id}")]
    public async Task<ActionResult> UpdateProduct(Guid id, [FromBody] AdminUpdateProductRequest request)
    {
        try
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (request.Name != null) product.Name = request.Name;
            if (request.Description != null) product.Description = request.Description;
            if (request.PricePerUnit.HasValue) product.PricePerUnit = request.PricePerUnit.Value;
            if (request.StockQuantity.HasValue)
            {
                product.StockQuantity = request.StockQuantity.Value;
                product.Status = product.StockQuantity > product.LowStockThreshold ? "available" :
                                 product.StockQuantity > 0 ? "low_stock" : "out_of_stock";
            }
            if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Product updated" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Farmers & Categories (for dropdowns) ─────────────────

    [HttpGet("farmers")]
    public async Task<ActionResult> GetFarmers()
    {
        var farmers = await _db.FarmerProfiles
            .Select(f => new { f.Id, f.FarmName, f.OwnerName })
            .ToListAsync();
        return Ok(farmers);
    }

    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories()
    {
        var categories = await _db.ProductCategories
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        return Ok(categories);
    }

    // ── Notifications (low stock alerts) ─────────────────────

    [HttpGet("notifications")]
    public async Task<ActionResult> GetNotifications()
    {
        var lowStock = await _db.Products
            .Include(p => p.Farm)
            .Where(p => p.IsActive && (p.Status == "low_stock" || p.Status == "out_of_stock"))
            .Select(p => new {
                p.Id,
                p.Name,
                p.StockQuantity,
                p.Status,
                p.Unit,
                FarmName = p.Farm.FarmName,
                Type = p.Status == "out_of_stock" ? "Out of Stock" : "Low Stock",
                Message = p.Status == "out_of_stock"
                    ? p.Name + " is out of stock at " + p.Farm.FarmName
                    : p.Name + " is running low (" + p.StockQuantity + " " + p.Unit + " left)"
            })
            .ToListAsync();
        return Ok(lowStock);
    }

    // ── Overview Stats ───────────────────────────────────────

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var totalProducts = await _db.Products.CountAsync(p => p.IsActive);
        var totalFarmers = await _db.FarmerProfiles.CountAsync();
        var totalBuyers = await _db.BuyerProfiles.CountAsync();
        var totalOrders = await _db.Orders.CountAsync();
        var lowStockCount = await _db.Products.CountAsync(p => p.IsActive && p.Status == "low_stock");
        var outOfStockCount = await _db.Products.CountAsync(p => p.IsActive && p.Status == "out_of_stock");

        return Ok(new
        {
            totalProducts,
            totalFarmers,
            totalBuyers,
            totalOrders,
            lowStockCount,
            outOfStockCount
        });
    }
}

public class AdminCreateProductRequest
{
    public Guid FarmId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerUnit { get; set; }
    public string? Unit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal? Capacity { get; set; }
    public decimal LowStockThreshold { get; set; } = 10;
}

public class AdminUpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerUnit { get; set; }
    public decimal? StockQuantity { get; set; }
    public bool? IsActive { get; set; }
}