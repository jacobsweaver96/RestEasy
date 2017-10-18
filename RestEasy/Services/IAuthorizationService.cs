using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestEasy.Services
{
    /// <summary>
    /// Permissions levels
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>
        /// No permissions
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Read permissions
        /// </summary>
        READ = 1,
        /// <summary>
        /// Write permissions
        /// </summary>
        WRITE = 2,
        /// <summary>
        /// All permissions up to altering clients
        /// </summary>
        ADMIN = 3
    }

    /// <summary>
    /// Interface for authorizing clients
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Determine whether or not a client is authorized to perform an action
        /// </summary>
        /// <param name="clientKey">The client's identifying key</param>
        /// <param name="requiredPermissionLevel">The required permissions to perform the action</param>
        /// <returns>Authorization determinator</returns>
        Task<bool> Authorize(string clientKey, PermissionLevel requiredPermissionLevel);
        /// <summary>
        /// Determine whether or not a client is authorized to perform an action
        /// </summary>
        /// <param name="clientKey">The client's identifying key</param>
        /// <param name="requiredPermissionLevels">The required permissions to perform the action</param>
        /// <returns>Authorization determinator</returns>
        Task<bool> Authorize(string clientKey, List<PermissionLevel> requiredPermissionLevels);
    }
}
