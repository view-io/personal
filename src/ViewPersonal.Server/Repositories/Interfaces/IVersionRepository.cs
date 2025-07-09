using ViewPersonal.Server.Models;

namespace ViewPersonal.Server.Repositories.Interfaces
{
    /// <summary>
    /// Interface for version repository operations
    /// </summary>
    public interface IVersionRepository
    {
        /// <summary>
        /// Gets all versions
        /// </summary>
        /// <returns>A collection of all versions</returns>
        Task<IEnumerable<AppVersion>> GetAllVersionsAsync();

        /// <summary>
        /// Gets the latest version
        /// </summary>
        /// <returns>The latest version or null if no versions exist</returns>
        Task<AppVersion?> GetLatestVersionAsync();

        /// <summary>
        /// Gets a version by its ID
        /// </summary>
        /// <param name="id">The ID of the version to get</param>
        /// <returns>The version with the specified ID or null if not found</returns>
        Task<AppVersion?> GetVersionByIdAsync(int id);

        /// <summary>
        /// Adds a new version
        /// </summary>
        /// <param name="version">The version to add</param>
        /// <returns>The added version</returns>
        Task<AppVersion> AddVersionAsync(AppVersion version);

        /// <summary>
        /// Updates an existing version
        /// </summary>
        /// <param name="version">The version to update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        Task<bool> UpdateVersionAsync(AppVersion version);

        /// <summary>
        /// Deletes a version by its ID
        /// </summary>
        /// <param name="id">The ID of the version to delete</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        Task<bool> DeleteVersionAsync(int id);
    }
}