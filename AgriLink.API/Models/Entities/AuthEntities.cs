using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriLink.API.Models.Entities;

// ── Enums ────────────────────────────────────────────────────

public enum UserRole { admin, farmer, buyer }
public enum BuyerTypeEnum { household, restaurant, market }
public enum PermitStatus { pending, approved, rejected }

// ── Users Table ──────────────────────────────────────────────

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    public UserRole Role { get; set; }

    [MaxLength(20)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_verified")]
    public bool IsVerified { get; set; } = false;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (links to other tables)
    public FarmerProfile? FarmerProfile { get; set; }
    public BuyerProfile? BuyerProfile { get; set; }
}

// ── Farmer Profile Table ─────────────────────────────────────

[Table("farmer_profiles")]
public class FarmerProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required, MaxLength(255)]
    [Column("farm_name")]
    public string FarmName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Column("owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [Column("farm_description")]
    public string? FarmDescription { get; set; }

    [Required]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Column("latitude")]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    public decimal? Longitude { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}

// ── Buyer Profile Table ──────────────────────────────────────

[Table("buyer_profiles")]
public class BuyerProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required, MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("buyer_type_id")]
    public Guid BuyerTypeId { get; set; }

    [Column("delivery_address")]
    public string? DeliveryAddress { get; set; }

    [Column("is_permit_verified")]
    public bool IsPermitVerified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public BuyerType BuyerType { get; set; } = null!;
}

// ── Buyer Types Lookup Table ─────────────────────────────────

[Table("buyer_types")]
public class BuyerType
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("name")]
    public BuyerTypeEnum Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("requires_permit")]
    public bool RequiresPermit { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}