using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Services;

// ── DTOs ─────────────────────────────────────────────────────

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerUnit { get; set; }
    public string Unit { get; set; } = "kg";
    public decimal StockQuantity { get; set; }
    public decimal? Capacity { get; set; }
    public string Status { get; set; } = "available";
    public string? ImageUrl { get; set; }
    public string? SeedingDate { get; set; }
    public string? HarvestDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FarmName { get; set; } = string.Empty;
}

// ── Interface ────────────────────────────────────────────────

public interface IProductService
{
    Task<List<ProductDto>> GetFarmerProducts(Guid userId);
}

// ── Implementation ───────────────────────────────────────────

public class ProductService : IProductService
{
    private readonly AgriLinkDbContext _db;

    public ProductService(AgriLinkDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductDto>> GetFarmerProducts(Guid userId)
    {
        var farmer = await _db.FarmerProfiles
            .FirstOrDefaultAsync(f => f.UserId == userId);

        if (farmer == null)
            throw new Exception("Farmer profile not found");

        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Farm)
            .Where(p => p.FarmId == farmer.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            PricePerUnit = p.PricePerUnit,
            Unit = p.Unit,
            StockQuantity = p.StockQuantity,
            Capacity = p.Capacity,
            Status = p.Status,
            ImageUrl = p.ImageUrl,
            SeedingDate = p.SeedingDate?.ToString("MM/dd/yyyy"),
            HarvestDate = p.HarvestDate?.ToString("MM/dd/yyyy"),
            CategoryName = p.Category?.Name ?? "",
            FarmName = p.Farm?.FarmName ?? ""
        }).ToList();
    }
}