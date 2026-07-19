using System;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeShareAPI.Services
{
    public class RankService : IRankService
    {
        private readonly ApplicationDbContext _context;

        public RankService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddEmbersAsync(Guid userId, string actionType, Guid targetId, int points)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Anti-Spam: Check if this exact action/target has already been rewarded
                var existingTransaction = await _context.EmberTransactions
                    .AnyAsync(t => t.UserId == userId && t.ActionType == actionType && t.TargetId == targetId);
                
                if (existingTransaction)
                {
                    return false; // Already rewarded
                }

                // Create Transaction Record
                var newTransaction = new EmberTransaction
                {
                    UserId = userId,
                    ActionType = actionType,
                    TargetId = targetId,
                    Points = points,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.EmberTransactions.Add(newTransaction);

                // Award Points
                user.TotalEmbers += points;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Check for Rank Up asynchronously (don't block the points award if it fails)
                await CheckRankUpAsync(userId);
                
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CheckRankUpAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Get all ranks sorted by RequiredEmbers descending
            var eligibleRank = await _context.Ranks
                .Where(r => r.RequiredEmbers <= user.TotalEmbers)
                .OrderByDescending(r => r.RequiredEmbers)
                .FirstOrDefaultAsync();

            if (eligibleRank != null && user.RankId != eligibleRank.Id)
            {
                user.RankId = eligibleRank.Id;
                await _context.SaveChangesAsync();
                return true; // Ranked up (or down)
            }

            return false;
        }
    }
}
