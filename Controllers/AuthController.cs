using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CORE_BE;
using CORE_BE.Data;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[EnableCors("CorsApi")]
[AllowAnonymous]
[ApiController]
[Route("token")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration config,
        UserManager<ApplicationUser> userMgr,
        SignInManager<ApplicationUser> signInMgr,
        ILogger<AuthController> logger
    )
    {
        _userManager = userMgr;
        _signInManager = signInMgr;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generate JWT token
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Login(LoginModel request)
    {
        var user = new ApplicationUser();
        var email = "";
        if (request.Domain != null)
        {
            email = request.Username + "@" + request.Domain;
            user = await _userManager.FindByEmailAsync(email);
        }
        else
        {
            email = request.Username;
            user = await _userManager.FindByNameAsync(request.Username);
        }
        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for {Username}", request.Username);
            return Unauthorized(new { message = "Thông tin đăng nhập không đúng" });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: account disabled for {Username}", request.Username);
            return Unauthorized(new { message = "Tài khoản đã bị khóa" });
        }

        if (IsValidEmail(email))
        {
            var isLoginedByEmail = false;
            var useMailExchange = _config.GetValue<bool>("UseMailExchange");
            if (useMailExchange && (request.Domain == "thaco.com.vn"))
            {
                isLoginedByEmail = Commons.LoginExchange(email, request.Password);
            }
            else
            {
                isLoginedByEmail = Commons.LoginLDAP(
                    request.Username,
                    request.Password,
                    request.Domain,
                    _config
                );
            }
            if (isLoginedByEmail)
            {
                // Update password same password login mail Thaco
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(
                    user,
                    request.Password
                );
                user.MustChangePass = false;
                var updatePass = await _userManager.UpdateAsync(user);
                if (updatePass.Succeeded)
                {
                    return Ok(
                        GenToken(
                            new UserToken
                            {
                                Id = user.Id.ToString(),
                                Email = user.Email,
                                FullName = user.FullName,
                                MustChangePass = user.MustChangePass,
                                AvatarUrl = user.AvatarUrl,
                            }
                        )
                    );
                }
                else
                {
                    _logger.LogError("Password sync failed for user {Username}", request.Username);
                    return BadRequest(new { message = "Đồng bộ mật khẩu tài khoản thất bại" });
                }
            }
            else
            {
                return Unauthorized(new { message = "Thông tin đăng nhập không đúng" });
            }
        }
        else
        {
            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );
            if (result.Succeeded)
            {
                var tokenResult = GenToken(
                    new UserToken
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        FullName = user.FullName,
                        MustChangePass = user.MustChangePass,
                        AvatarUrl = user.AvatarUrl,
                    }
                );

                user.RefreshToken = tokenResult.RefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                return Ok(tokenResult);
            }
            else
            {
                return Unauthorized(new { message = "Thông tin đăng nhập không đúng" });
            }
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(TokenApiModel tokenApiModel)
    {
        if (tokenApiModel is null)
        {
            return BadRequest("Invalid client request");
        }

        var accessToken = tokenApiModel.AccessToken;
        var refreshToken = tokenApiModel.RefreshToken;

        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return BadRequest("Invalid token claims");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        var newAccessToken = GenToken(new UserToken
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FullName = user.FullName,
            MustChangePass = user.MustChangePass,
            AvatarUrl = user.AvatarUrl
        });

        user.RefreshToken = newAccessToken.RefreshToken;
        await _userManager.UpdateAsync(user);

        return Ok(newAccessToken);
    }

    private InfoLogin GenToken(UserToken userToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["AppSettings:Secret"]);
        var expireMinutes = _config.GetValue<int>("AppSettings:ExpireMinutes", 60);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["AppSettings:Issuer"],
            Audience = _config["AppSettings:Audience"],
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userToken.Id),
                    new Claim(ClaimTypes.Name, userToken.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("email", userToken.Email ?? ""),
                    new Claim("FullName", userToken.FullName ?? ""),
                    new Claim("MustChangePass", userToken.MustChangePass.ToString()),
                    new Claim("AvatarUrl", userToken.AvatarUrl ?? ""),
                }
            ),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        return new InfoLogin()
        {
            Token = tokenHandler.WriteToken(token),
            RefreshToken = refreshToken,
            Id = userToken.Id,
            Email = userToken.Email,
            FullName = userToken.FullName,
            Expires = token.ValidTo,
            MustChangePass = userToken.MustChangePass,
            AvatarUrl = userToken.AvatarUrl,
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // audience/issuer validation can be skipped since we just need claims
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AppSettings:Secret"])),
            ValidateLifetime = false // we want to extract claims from an expired token
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
