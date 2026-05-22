using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Controllers;

[ApiController]
[Route("api/buyer-orders")]
[Authorize]
public class BuyerOrdersController : ControllerBase
{
    private readonly AgriLinkDbContext _db;

    public BuyerOrdersController(AgriLinkDbContext db)
    {
        _db = db;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var buyer = await _db.BuyerProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
            if (buyer == null) return BadRequest(new { message = "Buyer profile not found" });

            // Get cart items with products
            var cartItems = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.BuyerId == buyer.Id)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { message = "Cart is empty" });

            // Group by farmer
            var farmerGroups = cartItems.GroupBy(ci => ci.Product.FarmId);

            var createdOrders = new List<object>();

            foreach (var group in farmerGroups)
            {
                var farmerId = group.Key;
                var items = group.ToList();
                var subtotal = items.Sum(i => i.Quantity * i.Product.PricePerUnit);
                var deliveryFee = 85.00m;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "AL-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                    BuyerId = buyer.Id,
                    FarmerId = farmerId,
                    Status = "pending",
                    PaymentMethod = request.PaymentMethod ?? "gcash",
                    DeliveryAddress = request.DeliveryAddress ?? buyer.DeliveryAddress ?? "",
                    Subtotal = subtotal,
                    DeliveryFee = deliveryFee,
                    TotalAmount = subtotal + deliveryFee,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                createdOrders.Add(new { order.Id, order.OrderNumber, order.TotalAmount });
            }

            // Clear cart
            _db.CartItems.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Order placed successfully!", orders = createdOrders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult> GetBuyerOrders([FromQuery] string? status = null)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var buyer = await _db.BuyerProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
            if (buyer == null) return BadRequest(new { message = "Buyer profile not found" });

            var query = _db.Orders
                .Include(o => o.Farmer)
                .Where(o => o.BuyerId == buyer.Id);

            if (!string.IsNullOrEmpty(status) && status != "all")
                query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var result = orders.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.PaymentMethod,
                o.DeliveryAddress,
                o.Subtotal,
                o.DeliveryFee,
                o.TotalAmount,
                FarmName = o.Farmer?.FarmName ?? "Unknown",
                o.CreatedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CheckoutRequest
{
    public string? PaymentMethod { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
}