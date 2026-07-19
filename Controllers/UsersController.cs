using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Data;
using Entities;
using Microsoft.AspNetCore.Authorization;

namespace CodeShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public UsersController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
        }

        [HttpGet("{username}/profile")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            var user = await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .Include(u => u.Rank)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound(new { Message = "User không tồn tại." });
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Bio,
                user.AvatarUrl,
                user.CoverUrl,
                user.SelectedBorderUrl,
                user.SelectedBannerUrl,
                FollowersCount = user.Followers.Count,
                FollowingCount = user.Following.Count,
                user.TotalEmbers,
                Rank = user.Rank == null ? null : new {
                    Id = user.Rank.Id,
                    user.Rank.Name,
                    user.Rank.RequiredEmbers,
                    user.Rank.SvgIcon
                }
            });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("{targetId}/toggle-follow")]
        public async Task<IActionResult> ToggleFollow(Guid targetId)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized(new { Message = "Token không hợp lệ." });
            }

            if (currentUserId == targetId)
            {
                return BadRequest(new { Message = "Bạn không thể tự follow chính mình." });
            }

            var targetUser = await _context.Users
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == targetId);

            if (targetUser == null)
            {
                return NotFound(new { Message = "Người dùng không tồn tại." });
            }

            var currentUser = await _context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            bool isFollowing = false;

            if (targetUser.Followers.Any(u => u.Id == currentUserId))
            {
                // Unfollow
                targetUser.Followers.Remove(currentUser!);
            }
            else
            {
                // Follow
                targetUser.Followers.Add(currentUser!);
                isFollowing = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = isFollowing ? "Đã theo dõi" : "Đã hủy theo dõi",
                IsFollowing = isFollowing
            });
        }

        public class UpdateIdentityRequest
        {
            public string? AvatarUrl { get; set; }
            public string? BorderUrl { get; set; }
            public string? BannerUrl { get; set; }
        }

        [HttpPut("update-identity")]
        [Authorize]
        public async Task<IActionResult> UpdateIdentity([FromBody] UpdateIdentityRequest request)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized(new { Message = "Token không hợp lệ." });
            }

            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null)
            {
                return NotFound(new { Message = "Người dùng không tồn tại." });
            }

            // --- Rank Requirements Map ---
            var borderRankReqs = new Dictionary<string, int>
            {
                { "border-default", 1 },
                { "border-infernal", 2 },
                { "thorn-knight", 3 },
                { "border-void", 4 },
                { "dark-crusader", 4 },
                { "umbral-priest", 4 },
                { "rhogar-warrior", 4 },
                { "radiant-vanguard", 5 },
                { "fallen-sentinel", 5 }
            };

            var bannerRankReqs = new Dictionary<string, int>
            {
                { "default", 1 },
                { "hellfire", 2 },
                { "abyssal", 3 },
                { "radiant", 4 },
                { "blood", 5 }
            };

            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl;
            
            if (!string.IsNullOrEmpty(request.BorderUrl))
            {
                if (borderRankReqs.TryGetValue(request.BorderUrl, out int reqRank))
                {
                    if (user.RankId < reqRank) return StatusCode(403, new { Message = $"Bạn cần đạt Rank {reqRank} để mở khóa Khung này!" });
                }
                user.SelectedBorderUrl = request.BorderUrl;
            }
                
            if (!string.IsNullOrEmpty(request.BannerUrl))
            {
                if (bannerRankReqs.TryGetValue(request.BannerUrl, out int reqRank))
                {
                    if (user.RankId < reqRank) return StatusCode(403, new { Message = $"Bạn cần đạt Rank {reqRank} để mở khóa Cờ này!" });
                }
                user.SelectedBannerUrl = request.BannerUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Cập nhật định danh thành công.",
                user.AvatarUrl,
                user.SelectedBorderUrl,
                user.SelectedBannerUrl
            });
        }
        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Message = "Token không hợp lệ." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "File không hợp lệ." });
            }

            var account = new Account(
                _configuration["CloudinarySettings:CloudName"],
                _configuration["CloudinarySettings:ApiKey"],
                _configuration["CloudinarySettings:ApiSecret"]
            );
            var cloudinary = new Cloudinary(account);

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "BonfireCode/system_avatars"
            };
            
            var uploadResult = await cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var avatarUrl = uploadResult.SecureUrl.ToString();
                return Ok(new { AvatarUrl = avatarUrl });
            }

            return BadRequest(new { Message = "Upload lên Cloudinary thất bại." });
        }
    }
}
