using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Services;

namespace AgriLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private readonly AgriLinkDbContext _db;

    public MarketplaceController(AgriLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet("products")]
    public async Task<ActionResult> GetProducts([FromQuery] string? search = null, [FromQuery] string? category = null)
    {
        try
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Farm)
                .Where(p => p.IsActive && p.Status != "out_of_stock");

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Category.Name.ToLower().Contains(search) ||
                    p.Farm.FarmName.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.Name.ToLower() == category.ToLower());
            }

            var products = await query.OrderBy(p => p.Name).ToListAsync();

            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.PricePerUnit,
                p.Unit,
                p.StockQuantity,
                p.Status,
                p.ImageUrl,
                CategoryName = p.Category?.Name ?? "",
                FarmName = p.Farm?.FarmName ?? "",
                FarmerName = p.Farm?.OwnerName ?? "",
                FarmAddress = p.Farm?.Address ?? "",
                Latitude = p.Farm?.Latitude,
                Longitude = p.Farm?.Longitude
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetMarketplaceStats()
    {
        try
        {
            var activeFarmers = await _db.FarmerProfiles.CountAsync();
            var availableProducts = await _db.Products.CountAsync(p => p.IsActive && p.Status != "out_of_stock");

            return Ok(new
            {
                activeFarmers,
                availableProducts
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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