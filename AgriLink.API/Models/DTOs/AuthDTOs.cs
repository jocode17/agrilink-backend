namespace AgriLink.API.Models.DTOs;

// ── Login ────────────────────────────────────────────────────

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// ── Register Farmer ──────────────────────────────────────────

public class RegisterFarmerRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FarmName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? FarmDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

// ── Register Buyer ───────────────────────────────────────────

public class RegisterBuyerRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string BuyerType { get; set; } = string.Empty;  // "household", "restaurant", "market"
    public string DeliveryAddress { get; set; } = string.Empty;
}

// ── Auth Response (returned after login/register) ────────────

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

// ── User DTO ─────────────────────────────────────────────────

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsVerified { get; set; }
    public object? Profile { get; set; }  // FarmerProfileDto or BuyerProfileDto
}

// ── Farmer Profile DTO ───────────────────────────────────────

public class FarmerProfileDto
{
    public Guid Id { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? FarmDescription { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsVerified { get; set; }
}

// ── Buyer Profile DTO ────────────────────────────────────────

public class BuyerProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BuyerType { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public bool IsPermitVerified { get; set; }
}