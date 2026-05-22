using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Controllers;

[ApiController]
[Route("api/farmer-products")]
[Authorize]
public class FarmerProductsController : ControllerBase
{
    private readonly AgriLinkDbContext _db;

    public FarmerProductsController(AgriLinkDbContext db)
    {
        _db = db;
    }

    // GET /api/farmer-products — list products for logged-in farmer
    [HttpGet]
    public async Task<ActionResult> GetMyProducts()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var farm = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound(new { message = "Farm profile not found" });

            var products = await _db.Products
                .Where(p => p.FarmId == farm.Id && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.PricePerUnit,
                    p.Unit,
                    p.StockQuantity,
                    p.Capacity,
                    p.LowStockThreshold,
                    p.Status,
                    p.ImageUrl,
                    p.IsActive,
                    p.SeedingDate,
                    p.HarvestDate,
                    CategoryName = p.Category != null ? p.Category.Name : "",
                    CategoryId = p.CategoryId,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/farmer-products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult> GetProduct(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var farm = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound(new { message = "Farm profile not found" });

            var product = await _db.Products
                .Where(p => p.Id == id && p.FarmId == farm.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.PricePerUnit,
                    p.Unit,
                    p.StockQuantity,
                    p.Capacity,
                    p.LowStockThreshold,
                    p.Status,
                    p.ImageUrl,
                    p.IsActive,
                    p.SeedingDate,
                    p.HarvestDate,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .FirstOrDefaultAsync();

            if (product == null) return NotFound(new { message = "Product not found" });
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/farmer-products — create a new product
    [HttpPost]
    public async Task<ActionResult> CreateProduct([FromBody] CreateFarmerProductRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var farm = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound(new { message = "Farm profile not found" });

            var product = new Product
            {
                Id = Guid.NewGuid(),
                AdminId = userId,
                FarmId = farm.Id,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Description = request.Description ?? "",
                PricePerUnit = request.PricePerUnit,
                Unit = request.Unit ?? "kg",
                StockQuantity = request.StockQuantity,
                Capacity = request.Capacity,
                LowStockThreshold = request.LowStockThreshold ?? 10,
                Status = request.StockQuantity > 0 ? "available" : "out_of_stock",
                ImageUrl = request.ImageUrl ?? "",
                SeedingDate = request.SeedingDate,
                HarvestDate = request.HarvestDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                product.Id,
                product.Name,
                product.PricePerUnit,
                product.StockQuantity,
                product.Status,
                message = "Product created successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT /api/farmer-products/{id} — update a product
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(Guid id, [FromBody] UpdateFarmerProductRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var farm = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound(new { message = "Farm profile not found" });

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmId == farm.Id);
            if (product == null) return NotFound(new { message = "Product not found or not yours" });

            if (request.Name != null) product.Name = request.Name;
            if (request.Description != null) product.Description = request.Description;
            if (request.PricePerUnit.HasValue) product.PricePerUnit = request.PricePerUnit.Value;
            if (request.Unit != null) product.Unit = request.Unit;
            if (request.StockQuantity.HasValue)
            {
                product.StockQuantity = request.StockQuantity.Value;
                product.Status = request.StockQuantity.Value > 0 ? "available" : "out_of_stock";
            }
            if (request.Capacity.HasValue) product.Capacity = request.Capacity.Value;
            if (request.LowStockThreshold.HasValue) product.LowStockThreshold = request.LowStockThreshold.Value;
            if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
            if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
            if (request.SeedingDate.HasValue) product.SeedingDate = request.SeedingDate.Value;
            if (request.HarvestDate.HasValue) product.HarvestDate = request.HarvestDate.Value;

            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Product updated successfully", product.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /api/farmer-products/{id} — soft-delete a product
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var farm = await _db.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farm == null) return NotFound(new { message = "Farm profile not found" });

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmId == farm.Id);
            if (product == null) return NotFound(new { message = "Product not found or not yours" });

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/farmer-products/categories — get available categories
    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories()
    {
        try
        {
            var categories = await _db.ProductCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────

public class CreateFarmerProductRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PricePerUnit { get; set; }
    public string? Unit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal? Capacity { get; set; }
    public decimal? LowStockThreshold { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? SeedingDate { get; set; }
    public DateOnly? HarvestDate { get; set; }
}

public class UpdateFarmerProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerUnit { get; set; }
    public string? Unit { get; set; }
    public decimal? StockQuantity { get; set; }
    public decimal? Capacity { get; set; }
    public decimal? LowStockThreshold { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? SeedingDate { get; set; }
    public DateOnly? HarvestDate { get; set; }
}