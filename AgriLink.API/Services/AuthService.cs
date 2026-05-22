using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Helpers;
using AgriLink.API.Models.DTOs;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Services;

// ── Interface ────────────────────────────────────────────────

public interface IAuthService
{
    Task<AuthResponse> RegisterFarmer(RegisterFarmerRequest request);
    Task<AuthResponse> RegisterBuyer(RegisterBuyerRequest request);
    Task<AuthResponse> Login(LoginRequest request);
    Task<UserDto> GetCurrentUser(Guid userId);
}

// ── Implementation ───────────────────────────────────────────

public class AuthService : IAuthService
{
    private readonly AgriLinkDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthService(AgriLinkDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // ── Register Farmer ──────────────────────────────────────

    public async Task<AuthResponse> RegisterFarmer(RegisterFarmerRequest request)
    {
        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            throw new Exception("Email already registered");

        // Create the user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.farmer,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create the farmer profile
        var farmerProfile = new FarmerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FarmName = request.FarmName.Trim(),
            OwnerName = request.OwnerName.Trim(),
            FarmDescription = request.FarmDescription,
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.FarmerProfiles.Add(farmerProfile);
        await _db.SaveChangesAsync();

        // Generate JWT token
        var token = _jwt.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = MapToUserDto(user, farmerProfile, null)
        };
    }

    // ── Register Buyer ───────────────────────────────────────

    public async Task<AuthResponse> RegisterBuyer(RegisterBuyerRequest request)
    {
        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            throw new Exception("Email already registered");

        // Find the buyer type from the database
        var buyerTypeName = request.BuyerType.ToLower().Trim();
        BuyerTypeEnum buyerTypeEnum;

        if (!Enum.TryParse<BuyerTypeEnum>(buyerTypeName, true, out buyerTypeEnum))
            throw new Exception("Invalid buyer type. Must be: household, restaurant, or market");

        var buyerType = await _db.BuyerTypes
            .FirstOrDefaultAsync(bt => bt.Name == buyerTypeEnum);

        if (buyerType == null)
            throw new Exception("Buyer type not found in database");

        // Create the user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.buyer,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create the buyer profile
        var buyerProfile = new BuyerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            BuyerTypeId = buyerType.Id,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            IsPermitVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.BuyerProfiles.Add(buyerProfile);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = MapToUserDto(user, null, buyerProfile)
        };
    }

    // ── Login ────────────────────────────────────────────────

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        // Find user by email
        var user = await _db.Users
            .Include(u => u.FarmerProfile)
            .Include(u => u.BuyerProfile)
                .ThenInclude(bp => bp!.BuyerType)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user == null)
            throw new Exception("Invalid email or password");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid email or password");

        // Check if user is active
        if (!user.IsActive)
            throw new Exception("Account is deactivated");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            User = MapToUserDto(user, user.FarmerProfile, user.BuyerProfile)
        };
    }

    // ── Get Current User ─────────────────────────────────────

    public async Task<UserDto> GetCurrentUser(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.FarmerProfile)
            .Include(u => u.BuyerProfile)
                .ThenInclude(bp => bp!.BuyerType)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("User not found");

        return MapToUserDto(user, user.FarmerProfile, user.BuyerProfile);
    }

    // ── Helper: Map User to DTO ──────────────────────────────

    private UserDto MapToUserDto(User user, FarmerProfile? farmer, BuyerProfile? buyer)
    {
        object? profile = null;

        if (farmer != null)
        {
            profile = new FarmerProfileDto
            {
                Id = farmer.Id,
                FarmName = farmer.FarmName,
                OwnerName = farmer.OwnerName,
                FarmDescription = farmer.FarmDescription,
                Address = farmer.Address,
                Latitude = farmer.Latitude,
                Longitude = farmer.Longitude,
                IsVerified = farmer.IsVerified
            };
        }
        else if (buyer != null)
        {
            profile = new BuyerProfileDto
            {
                Id = buyer.Id,
                FullName = buyer.FullName,
                BuyerType = buyer.BuyerType?.Name.ToString() ?? "household",
                DeliveryAddress = buyer.DeliveryAddress,
                IsPermitVerified = buyer.IsPermitVerified
            };
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            PhoneNumber = user.PhoneNumber,
            IsVerified = user.IsVerified,
            Profile = profile
        };
    }
}