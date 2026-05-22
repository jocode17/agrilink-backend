using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriLink.API.Models.Entities;

[Table("conversations")]
public class Conversation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("farmer_id")]
    public Guid FarmerId { get; set; }

    [Column("buyer_id")]
    public Guid BuyerId { get; set; }

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FarmerProfile Farmer { get; set; } = null!;
    public BuyerProfile Buyer { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

[Table("messages")]
public class Message
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    [Column("sender_id")]
    public Guid SenderId { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Conversation Conversation { get; set; } = null!;
}