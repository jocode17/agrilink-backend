using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Controllers;

// ── Cart Item Entity ─────────────────────────────────────────
// Add this to Models/Entities if not already there

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AgriLinkDbContext _db;

    public CartController(AgriLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetCart()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var buyer = await _db.BuyerProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
            if (buyer == null) return BadRequest(new { message = "Buyer profile not found" });

            var items = await _db.CartItems
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Farm)
                .Where(ci => ci.BuyerId == buyer.Id)
                .Select(ci => new
                {
                    ci.Id,
                    ci.ProductId,
                    ProductName = ci.Product.Name,
                    ci.Product.ImageUrl,
                    ci.Product.PricePerUnit,
                    ci.Product.Unit,
                    ci.Quantity,
                    Subtotal = ci.Quantity * ci.Product.PricePerUnit,
                    FarmName = ci.Product.Farm.FarmName,
                    FarmerId = ci.Product.Farm.Id
                })
                .ToListAsync();

            return Ok(items);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var buyer = await _db.BuyerProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
            if (buyer == null) return BadRequest(new { message = "Buyer profile not found" });

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.BuyerId == buyer.Id && ci.ProductId == request.ProductId);

            if (existing != null)
            {
                existing.Quantity += request.Quantity;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Added to cart" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuantity(Guid id, [FromBody] UpdateCartQuantity request)
    {
        try
        {
            var item = await _db.CartItems.FindAsync(id);
            if (item == null) return NotFound();

            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Updated" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RemoveItem(Guid id)
    {
        try
        {
            var item = await _db.CartItems.FindAsync(id);
            if (item == null) return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Removed" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
}

public class UpdateCartQuantity
{
    public decimal Quantity { get; set; }
}