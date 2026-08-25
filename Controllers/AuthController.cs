using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeShareAPI.Data;
using CodeShareAPI.Entities;
using CodeShareAPI.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CodeShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // --- EMAIL SENDER LOGIC ---
        private async Task SendEmailAsync(string toEmail, string otp)
        {
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            
            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                throw new Exception("Email settings are not configured in appsettings.");
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "BonfireCode Support"),
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

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
        // -------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email đã được sử dụng.");
            }
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Tên đăng nhập đã được sử dụng.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AvatarUrl = string.IsNullOrEmpty(request.AvatarUrl) ? "https://res.cloudinary.com/dwyx971u2/image/upload/v1721544654/default-avatar.png" : request.AvatarUrl
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Email hoặc mật khẩu không chính xác.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Always return Ok to prevent email enumeration
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

            return Ok(new { Message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư." });
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

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.AvatarUrl,
                user.Role,
                user.Embers,
                user.Rank
            });
        }
    }
}