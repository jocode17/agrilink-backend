using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriLink.API.Models.Entities;

[Table("cart_items")]
public class CartItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("buyer_id")]
    public Guid BuyerId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BuyerProfile Buyer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}