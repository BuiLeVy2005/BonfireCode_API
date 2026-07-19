using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Data;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiuaKy.Controllers
{
    [Route("api/ratings")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CodeShareAPI.Services.IRankService _rankService;

        public RatingController(ApplicationDbContext context, CodeShareAPI.Services.IRankService rankService)
        {
            _context = context;
            _rankService = rankService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitRating([FromBody] SubmitRatingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.StarValue < 1 || request.StarValue > 5)
                {
                    return BadRequest(new { Message = "Số sao (StarValue) phải từ 1 đến 5." });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                                ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                                ?? User.FindFirstValue("nameid");

                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                {
                    return Unauthorized(new { Message = "Không thể xác thực người dùng." });
                }

                var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
                if (!projectExists)
                {
                    return NotFound(new { Message = "Project không tồn tại." });
                }

                var existingRating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.ProjectId == request.ProjectId && r.UserId == userId);

                if (existingRating != null)
                {
                    // Cập nhật rating cũ
                    existingRating.StarValue = request.StarValue;
                    existingRating.CreatedAt = DateTime.UtcNow; // Cập nhật thời gian rate
                    _context.Ratings.Update(existingRating);
                }
                else
                {
                    // Thêm mới
                    var newRating = new Rating
                    {
                        ProjectId = request.ProjectId,
                        UserId = userId,
                        StarValue = request.StarValue,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Ratings.Add(newRating);
                }

                await _context.SaveChangesAsync();

                // Award 5 Embers for rating (only once per project/user via Anti-Spam in RankService)
                await _rankService.AddEmbersAsync(userId, "RATE", request.ProjectId, 5);

                return Ok(new { Message = "Gửi đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server trong quá trình xử lý đánh giá.", Details = ex.Message });
            }
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetProjectRating(Guid projectId)
        {
            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
                if (!projectExists)
                {
                    return NotFound(new { Message = "Project không tồn tại." });
                }

                var ratings = await _context.Ratings
                    .Where(r => r.ProjectId == projectId)
                    .ToListAsync();

                if (!ratings.Any())
                {
                    return Ok(new
                    {
                        Average = 0.0,
                        TotalRatings = 0
                    });
                }

                var average = ratings.Average(r => r.StarValue);
                var totalRatings = ratings.Count;

                return Ok(new
                {
                    Average = Math.Round(average, 1),
                    TotalRatings = totalRatings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server trong quá trình lấy thống kê đánh giá.", Details = ex.Message });
            }
        }
    }

    public class SubmitRatingRequest
    {
        public Guid ProjectId { get; set; }
        public int StarValue { get; set; }
    }
}
