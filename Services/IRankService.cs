using System;
using System.Threading.Tasks;

namespace CodeShareAPI.Services
{
    public interface IRankService
    {
        Task<bool> AddEmbersAsync(Guid userId, string actionType, Guid targetId, int points);
        Task<bool> CheckRankUpAsync(Guid userId);
    }
}
