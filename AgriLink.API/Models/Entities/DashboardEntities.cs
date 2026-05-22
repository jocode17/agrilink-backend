using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriLink.API.Models.Entities;

// ── Products ─────────────────────────────────────────────────

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("admin_id")]
    public Guid AdminId { get; set; }

    [Column("farm_id")]
    public Guid FarmId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = "kg";

    [Column("stock_quantity")]
    public decimal StockQuantity { get; set; }

    [Column("capacity")]
    public decimal? Capacity { get; set; }

    [Column("low_stock_threshold")]
    public decimal LowStockThreshold { get; set; } = 10;

    [Column("status")]
    public string Status { get; set; } = "available";

    [MaxLength(500)]
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("seeding_date")]
    public DateOnly? SeedingDate { get; set; }

    [Column("harvest_date")]
    public DateOnly? HarvestDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FarmerProfile Farm { get; set; } = null!;
    public ProductCategory Category { get; set; } = null!;
}

// ── Product Categories ───────────────────────────────────────

[Table("product_categories")]
public class ProductCategory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Orders ───────────────────────────────────────────────────

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("buyer_id")]
    public Guid BuyerId { get; set; }

    [Column("farmer_id")]
    public Guid FarmerId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "gcash";

    [Column("delivery_address")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("delivery_fee")]
    public decimal DeliveryFee { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BuyerProfile Buyer { get; set; } = null!;
    public FarmerProfile Farmer { get; set; } = null!;
}

// ── Harvest Logs ─────────────────────────────────────────────

[Table("harvest_logs")]
public class HarvestLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("farm_id")]
    public Guid FarmId { get; set; }

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Required, MaxLength(255)]
    [Column("crop_name")]
    public string CropName { get; set; } = string.Empty;

    [Column("quantity_harvested")]
    public decimal QuantityHarvested { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = "kg";

    [Column("harvest_date")]
    public DateOnly HarvestDate { get; set; }

    [MaxLength(50)]
    [Column("season")]
    public string? Season { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FarmerProfile Farm { get; set; } = null!;
}