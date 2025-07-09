namespace SignalR
{
    public interface IUserCacheService
    {
        // Admins
        Task AddAdminConnectionAsync(string connectionId, string userId);
        Task RemoveAdminConnectionAsync(string connectionId);
        Task<List<string>> GetActiveAdminsAsync();

        // Clients (active users)
        Task AddActiveUserAsync(string connectionId, string userId);
        Task RemoveActiveUserAsync(string connectionId);
        Task<List<string>> GetActiveUsersAsync();

        // User role and userId getters
        Task<string?> GetUserRoleAsync(string connectionId);
        Task<string?> GetUserIdAsync(string connectionId);

        // Clear all user and admin cache data
        Task RemoveAllUserAsync();
    }

}
