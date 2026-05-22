using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AgriLink.API.Models.DTOs;
using AgriLink.API.Services;

namespace AgriLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new farmer account
    /// </summary>
    [HttpPost("register/farmer")]
    public async Task<ActionResult<AuthResponse>> RegisterFarmer([FromBody] RegisterFarmerRequest request)
    {
        try
        {
            var result = await _authService.RegisterFarmer(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Register a new buyer account
    /// </summary>
    [HttpPost("register/buyer")]
    public async Task<ActionResult<AuthResponse>> RegisterBuyer([FromBody] RegisterBuyerRequest request)
    {
        try
        {
            var result = await _authService.RegisterBuyer(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.Login(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current logged-in user's profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Not authenticated" });

            var userId = Guid.Parse(userIdClaim);
            var result = await _authService.GetCurrentUser(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}