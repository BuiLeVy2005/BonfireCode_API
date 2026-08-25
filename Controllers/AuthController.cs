using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace GiuaKy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        private async Task SendEmailAsync(string toEmail, string otp)
        {
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            
            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                throw new Exception("Email settings are not configured in appsettings.");
            }

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(senderEmail, "BonfireCode Support"),
                Subject = "Mã xác nhận khôi phục mật khẩu (OTP)",
                Body = $"<div style='font-family: Inter, Arial, sans-serif; padding: 20px; max-width: 600px; margin: 0 auto; border: 1px solid #30363d; border-radius: 10px; background-color: #0d1117; color: #c9d1d9;'>" +
                       $"<h2 style='color: #a855f7; text-align: center; font-size: 24px; font-weight: bold;'>BonfireCode</h2>" +
                       $"<p style='font-size: 16px;'>Chào bạn,</p>" +
                       $"<p style='font-size: 14px;'>Bạn vừa yêu cầu khôi phục mật khẩu. Dưới đây là mã OTP 6 chữ số của bạn:</p>" +
                       $"<div style='text-align: center; margin: 30px 0;'>" +
                       $"<span style='font-size: 28px; font-weight: 700; padding: 15px 30px; background-color: #161b22; border: 1px solid #30363d; border-radius: 8px; letter-spacing: 8px; color: #fff;'>{otp}</span>" +
                       $"</div>" +
                       $"<p style='font-size: 14px;'>Mã này sẽ <strong>hết hạn trong vòng 15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.</p>" +
                       $"<br><hr style='border: none; border-top: 1px solid #30363d;' />" +
                       $"<p style='font-size: 12px; color: #8b949e;'>Trân trọng,<br>Đội ngũ BonfireCode - Đồ án Niên luận Võ Chí Hải</p>" +
                       $"</div>",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(mailMessage);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Username đã tồn tại.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email đã tồn tại.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Role = "Student", // Mặc định là Student
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Always return Ok to prevent email enumeration, but we'll return OTP for testing purposes
                return Ok(new { Message = "Nếu email tồn tại, hệ thống đã gửi mã OTP." });
            }

            // Generate 6 digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            user.ResetToken = otp;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            
            await _context.SaveChangesAsync();

            try
            {
                await SendEmailAsync(user.Email, otp);
            }
            catch (Exception ex)
            {
                // In production, log the exception. Here we return 500 for clarity during presentation.
                return StatusCode(500, new { Message = "Lỗi khi kết nối với máy chủ gửi Email.", Error = ex.Message });
            }

            return Ok(new { Message = "Mã OTP đã được gửi đến email của bạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.ResetToken != request.Otp || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "Người dùng không tồn tại." });
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.FullName,
                user.Bio,
                user.AvatarUrl,
                user.CoverUrl,
                user.SelectedBorderUrl,
                user.CreatedAt
            });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "Người dùng không tồn tại." });
            }

            if (request.FullName != null) user.FullName = request.FullName;
            if (request.Bio != null) user.Bio = request.Bio;

            var account = new Account(
                _configuration["CloudinarySettings:CloudName"],
                _configuration["CloudinarySettings:ApiKey"],
                _configuration["CloudinarySettings:ApiSecret"]
            );
            var cloudinary = new Cloudinary(account);

            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                using var stream = request.AvatarFile.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(request.AvatarFile.FileName, stream),
                    Folder = "BonfireCode/avatars"
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    user.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            if (request.CoverFile != null && request.CoverFile.Length > 0)
            {
                using var stream = request.CoverFile.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(request.CoverFile.FileName, stream),
                    Folder = "BonfireCode/covers"
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    user.CoverUrl = uploadResult.SecureUrl.ToString();
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật hồ sơ thành công", AvatarUrl = user.AvatarUrl, CoverUrl = user.CoverUrl });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public IFormFile? AvatarFile { get; set; }
        public IFormFile? CoverFile { get; set; }
    }
}
