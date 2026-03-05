using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    public AuthController(
        IConfiguration config,
        UserManager<ApplicationUser> userMgr,
        SignInManager<ApplicationUser> signInMgr
    )
    {
        _userManager = userMgr;
        _signInManager = signInMgr;
        _config = config;
    }

    /// <summary>
    /// Generate JWT token
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Authencation(LoginModel request)
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
            // return BadRequest(new { message = "Tài Khoản không tồn tại" });
            return StatusCode(StatusCodes.Status204NoContent, "Tài khoản không tồn tại");
        else
        {
            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status204NoContent, "Tài khoản đã bị khóa");
            }
            else
            {
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
                            request.Domain
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
                        var update_pass = await _userManager.UpdateAsync(user);
                        if (update_pass.Succeeded)
                        {
                            return Ok(
                                GenToken(
                                    new UserToken
                                    {
                                        Id = user.Id.ToString(),
                                        Email = user.Email,
                                        FullName = user.FullName,
                                        MustChangePass = user.MustChangePass,
                                    }
                                )
                            );
                        }
                        else
                        {
                            return BadRequest("Đồng bộ mật khẩu tài khoản thất bại");
                        }
                    }
                    else
                    {
                        return BadRequest("Đăng nhập Email thất bại");
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
                        return Ok(
                            GenToken(
                                new UserToken
                                {
                                    Id = user.Id.ToString(),
                                    Email = user.Email,
                                    FullName = user.FullName,
                                    MustChangePass = user.MustChangePass,
                                }
                            )
                        );
                    }
                    else
                    {
                        return BadRequest("Thông tin đăng nhập không đúng");
                    }
                }
            }
        }
    }

    private InfoLogin GenToken(UserToken userToken)
    {
        // authentication successful so generate jwt token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["AppSettings:Secret"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["AppSettings:Issuer"],
            Audience = _config["AppSettings:Audience"],
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userToken.Id),
                    new Claim(ClaimTypes.Name, userToken.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("email", userToken.Email),
                    new Claim("FullName", userToken.FullName),
                }
            ),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new InfoLogin()
        {
            Token = tokenHandler.WriteToken(token),
            Id = userToken.Id,
            Email = userToken.Email,
            FullName = userToken.FullName,
            Expires = token.ValidTo,
            MustChangePass = userToken.MustChangePass,
        };
    }

    bool IsValidEmail(string email)
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
