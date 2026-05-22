using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Services;

// ── DTOs ─────────────────────────────────────────────────────

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerType { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// ── Interface ────────────────────────────────────────────────

public interface IOrderService
{
    Task<List<OrderDto>> GetFarmerOrders(Guid userId, string? status = null);
    Task<OrderDto> UpdateOrderStatus(Guid userId, Guid orderId, string newStatus);
}

// ── Implementation ───────────────────────────────────────────

public class OrderService : IOrderService
{
    private readonly AgriLinkDbContext _db;

    public OrderService(AgriLinkDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrderDto>> GetFarmerOrders(Guid userId, string? status = null)
    {
        var farmer = await _db.FarmerProfiles
            .FirstOrDefaultAsync(f => f.UserId == userId);

        if (farmer == null)
            throw new Exception("Farmer profile not found");

        var query = _db.Orders
            .Include(o => o.Buyer)
                .ThenInclude(b => b.BuyerType)
            .Where(o => o.FarmerId == farmer.Id);

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            DeliveryAddress = o.DeliveryAddress,
            Subtotal = o.Subtotal,
            DeliveryFee = o.DeliveryFee,
            TotalAmount = o.TotalAmount,
            Notes = o.Notes,
            BuyerName = o.Buyer?.FullName ?? "Unknown",
            BuyerType = o.Buyer?.BuyerType?.Name.ToString() ?? "household",
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OrderDto> UpdateOrderStatus(Guid userId, Guid orderId, string newStatus)
    {
        var farmer = await _db.FarmerProfiles
            .FirstOrDefaultAsync(f => f.UserId == userId);

        if (farmer == null)
            throw new Exception("Farmer profile not found");

        var order = await _db.Orders
            .Include(o => o.Buyer)
                .ThenInclude(b => b.BuyerType)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.FarmerId == farmer.Id);

        if (order == null)
            throw new Exception("Order not found");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Set user ID for the database trigger (order_status_history)
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_user_id', {0}, true)",
            userId.ToString());

        await _db.SaveChangesAsync();

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            DeliveryAddress = order.DeliveryAddress,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            TotalAmount = order.TotalAmount,
            BuyerName = order.Buyer?.FullName ?? "Unknown",
            BuyerType = order.Buyer?.BuyerType?.Name.ToString() ?? "household",
            CreatedAt = order.CreatedAt
        };
    }
}