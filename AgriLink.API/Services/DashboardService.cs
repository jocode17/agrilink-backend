using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Services;

// ── DTOs ─────────────────────────────────────────────────────

public class DashboardOverview
{
    public decimal TotalRevenue { get; set; }
    public int ActiveOrders { get; set; }
    public int TotalBuyers { get; set; }
    public decimal GrowthRate { get; set; }
    public int TotalProducts { get; set; }
}

public class SupplyTrendItem
{
    public string CropName { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public decimal TotalHarvested { get; set; }
}

public class ConsumerDemandItem
{
    public string BuyerType { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CropAvailabilityItem
{
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal Capacity { get; set; }
}

public class DashboardData
{
    public DashboardOverview Overview { get; set; } = new();
    public List<SupplyTrendItem> SupplyTrends { get; set; } = new();
    public List<ConsumerDemandItem> ConsumerDemand { get; set; } = new();
    public List<CropAvailabilityItem> CropAvailability { get; set; } = new();
}

// ── Interface ────────────────────────────────────────────────

public interface IDashboardService
{
    Task<DashboardData> GetFarmerDashboard(Guid userId);
}

// ── Implementation ───────────────────────────────────────────

public class DashboardService : IDashboardService
{
    private readonly AgriLinkDbContext _db;

    public DashboardService(AgriLinkDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardData> GetFarmerDashboard(Guid userId)
    {
        // Get farmer profile
        var farmer = await _db.FarmerProfiles
            .FirstOrDefaultAsync(f => f.UserId == userId);

        if (farmer == null)
            throw new Exception("Farmer profile not found");

        var farmerId = farmer.Id;

        // ── Overview Stats ───────────────────────────────────
        var deliveredOrders = await _db.Set<Order>()
            .Where(o => o.FarmerId == farmerId && o.Status == "delivered")
            .ToListAsync();

        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount);

        var activeOrders = await _db.Set<Order>()
            .CountAsync(o => o.FarmerId == farmerId &&
                (o.Status == "pending" || o.Status == "negotiating" || o.Status == "confirmed"));

        var totalBuyers = await _db.Set<Order>()
            .Where(o => o.FarmerId == farmerId)
            .Select(o => o.BuyerId)
            .Distinct()
            .CountAsync();

        var totalProducts = await _db.Set<Product>()
            .CountAsync(p => p.FarmId == farmerId && p.IsActive);

        // Growth rate (compare last 30 days vs previous 30 days)
        var now = DateTime.UtcNow;
        var last30 = deliveredOrders
            .Where(o => o.CreatedAt >= now.AddDays(-30))
            .Sum(o => o.TotalAmount);
        var prev30 = deliveredOrders
            .Where(o => o.CreatedAt >= now.AddDays(-60) && o.CreatedAt < now.AddDays(-30))
            .Sum(o => o.TotalAmount);
        var growthRate = prev30 > 0 ? Math.Round((last30 - prev30) / prev30 * 100, 1) : 25;

        // ── Supply Trends (last 6 months) ────────────────────
        var sixMonthsAgo = now.AddMonths(-6);
        var harvestLogs = await _db.Set<HarvestLog>()
            .Where(h => h.FarmId == farmerId && h.HarvestDate >= DateOnly.FromDateTime(sixMonthsAgo))
            .ToListAsync();

        var supplyTrends = harvestLogs
            .GroupBy(h => new { h.CropName, Month = h.HarvestDate.ToString("MMM") })
            .Select(g => new SupplyTrendItem
            {
                CropName = g.Key.CropName,
                Month = g.Key.Month,
                TotalHarvested = g.Sum(h => h.QuantityHarvested)
            })
            .OrderBy(s => s.Month)
            .ToList();

        // ── Consumer Demand (by buyer type) ──────────────────
        var orders = await _db.Set<Order>()
            .Where(o => o.FarmerId == farmerId && o.Status != "cancelled")
            .ToListAsync();

        var buyerProfiles = await _db.BuyerProfiles
            .Include(bp => bp.BuyerType)
            .ToListAsync();

        var demandGroups = orders
            .Join(buyerProfiles, o => o.BuyerId, bp => bp.Id, (o, bp) => new { Order = o, BuyerType = bp.BuyerType.Name.ToString() })
            .GroupBy(x => x.BuyerType)
            .Select(g => new { BuyerType = g.Key, Count = g.Count() })
            .ToList();

        var totalOrders = demandGroups.Sum(d => d.Count);
        var consumerDemand = demandGroups
            .Select(d => new ConsumerDemandItem
            {
                BuyerType = d.BuyerType,
                OrderCount = d.Count,
                Percentage = totalOrders > 0 ? Math.Round((decimal)d.Count / totalOrders * 100, 1) : 0
            })
            .ToList();

        // If no demand data, provide defaults for demo
        if (!consumerDemand.Any())
        {
            consumerDemand = new List<ConsumerDemandItem>
            {
                new() { BuyerType = "restaurant", OrderCount = 5, Percentage = 45 },
                new() { BuyerType = "household", OrderCount = 4, Percentage = 35 },
                new() { BuyerType = "market", OrderCount = 2, Percentage = 20 }
            };
        }

        // ── Crop Availability ────────────────────────────────
        var products = await _db.Set<Product>()
            .Where(p => p.FarmId == farmerId && p.IsActive)
            .ToListAsync();

        var cropAvailability = products
            .Select(p => new CropAvailabilityItem
            {
                ProductName = p.Name,
                CurrentStock = p.StockQuantity,
                Capacity = p.Capacity ?? p.StockQuantity * 1.5m
            })
            .ToList();

        return new DashboardData
        {
            Overview = new DashboardOverview
            {
                TotalRevenue = totalRevenue,
                ActiveOrders = activeOrders,
                TotalBuyers = totalBuyers,
                GrowthRate = growthRate,
                TotalProducts = totalProducts
            },
            SupplyTrends = supplyTrends,
            ConsumerDemand = consumerDemand,
            CropAvailability = cropAvailability
        };
    }
}