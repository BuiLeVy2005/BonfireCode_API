using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Data;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiuaKy.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CodeShareAPI.Services.IRankService _rankService;

        public CommentController(ApplicationDbContext context, CodeShareAPI.Services.IRankService rankService)
        {
            _context = context;
            _rankService = rankService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                                ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                                ?? User.FindFirstValue("nameid");

                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                {
                    return Unauthorized(new { Message = "Không thể xác thực người dùng." });
                }

                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId);
                if (project == null)
                {
                    return NotFound(new { Message = "Project không tồn tại." });
                }

                var comment = new Comment
                {
                    Content = request.Content,
                    ProjectId = request.ProjectId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);

                // Tạo thông báo cho chủ dự án
                if (project.UserId != userId)
                {
                    var currentUser = await _context.Users.FindAsync(userId);
                    _context.Notifications.Add(new Notification
                    {
                        UserId = project.UserId,
                        Title = "Bình luận mới",
                        Content = $"{currentUser?.Username ?? "Một người dùng"} đã bình luận vào đồ án {project.Title} của bạn.",
                        ActionUrl = $"/detail.html?id={project.Id}"
                    });
                }

                await _context.SaveChangesAsync();

                // Award 2 Embers for commenting
                await _rankService.AddEmbersAsync(userId, "COMMENT", comment.Id, 2);

                return Created($"/api/comments/{comment.ProjectId}", new { Message = "Thêm bình luận thành công.", CommentId = comment.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server trong quá trình thêm bình luận.", Details = ex.Message });
            }
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetComments(Guid projectId)
        {
            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
                if (!projectExists)
                {
                    return NotFound(new { Message = "Project không tồn tại." });
                }

                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.ProjectId == projectId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Content,
                        c.CreatedAt,
                        AuthorName = c.User.Username
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server trong quá trình lấy bình luận.", Details = ex.Message });
            }
        }
    }

    public class CreateCommentRequest
    {
        public Guid ProjectId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
