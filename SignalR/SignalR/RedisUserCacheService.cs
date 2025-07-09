using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace SignalR
{
    public class RedisUserCacheService : IUserCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        private const string AdminsSetKey = "active-admins"; // Redis set key for admins
        private const string ActiveUsersSetKey = "active-users"; // Redis set key for users

        public RedisUserCacheService(IDistributedCache cache)
        {
            _cache = cache;
            _redis = ConnectionMultiplexer.Connect("localhost");
            _db = _redis.GetDatabase();
        }

        // Add admin userId to Redis set
        public async Task AddAdminConnectionAsync(string connectionId, string userId)
        {
            await _db.SetAddAsync(AdminsSetKey, userId);
            await _cache.SetStringAsync($"role:{connectionId}", "admin");
            await _cache.SetStringAsync($"userId:{connectionId}", userId);
        }

        // Remove admin userId from Redis set
        public async Task RemoveAdminConnectionAsync(string connectionId)
        {
            var userId = await _cache.GetStringAsync($"userId:{connectionId}");
            if (!string.IsNullOrEmpty(userId))
            {
                await _db.SetRemoveAsync(AdminsSetKey, userId);
            }
            await _cache.RemoveAsync($"role:{connectionId}");
            await _cache.RemoveAsync($"userId:{connectionId}");
        }

        // Add client userId to Redis set
        public async Task AddActiveUserAsync(string connectionId, string userId)
        {
            await _db.SetAddAsync(ActiveUsersSetKey, userId);
            await _cache.SetStringAsync($"role:{connectionId}", "client");
            await _cache.SetStringAsync($"userId:{connectionId}", userId);
        }

        // Remove client userId from Redis set
        public async Task RemoveActiveUserAsync(string connectionId)
        {
            var userId = await _cache.GetStringAsync($"userId:{connectionId}");
            if (!string.IsNullOrEmpty(userId))
            {
                await _db.SetRemoveAsync(ActiveUsersSetKey, userId);
            }
            await _cache.RemoveAsync($"role:{connectionId}");
            await _cache.RemoveAsync($"userId:{connectionId}");
        }

        // Get all active userIds
        public async Task<List<string>> GetActiveUsersAsync()
        {
            var users = await _db.SetMembersAsync(ActiveUsersSetKey);
            return users.Select(u => u.ToString()).ToList();
        }

        // Get all active admin userIds
        public async Task<List<string>> GetActiveAdminsAsync()
        {
            var admins = await _db.SetMembersAsync(AdminsSetKey);
            return admins.Select(a => a.ToString()).ToList();
        }

        public async Task RemoveAllUserAsync()
        {
            var server = _redis.GetServer("localhost", 6379);

            var keys = server.Keys(pattern: "role:*").ToArray();
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }

            keys = server.Keys(pattern: "userId:*").ToArray();
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }

            await _db.KeyDeleteAsync(ActiveUsersSetKey);
            await _db.KeyDeleteAsync(AdminsSetKey);
        }

        public async Task<string?> GetUserRoleAsync(string connectionId)
        {
            return await _cache.GetStringAsync($"role:{connectionId}");
        }

        public async Task<string?> GetUserIdAsync(string connectionId)
        {
            return await _cache.GetStringAsync($"userId:{connectionId}");
        }
    }
}
