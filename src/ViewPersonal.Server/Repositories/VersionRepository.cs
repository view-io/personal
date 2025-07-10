namespace ViewPersonal.Server.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using ViewPersonal.Server.Data;
    using ViewPersonal.Server.Models;
    using ViewPersonal.Server.Repositories.Interfaces;

    /// <summary>
    /// Repository for version operations
    /// </summary>
    public class VersionRepository : IVersionRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionRepository"/> class
        /// </summary>
        /// <param name="context">The database context</param>
        public VersionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AppVersion>> GetAllVersionsAsync()
        {
            return await _context.Versions
                         .Include(v => v.OsDetails)
                         .OrderByDescending(v => v.ReleaseDate)
                         .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<AppVersion?> GetLatestVersionAsync()
        {
            return await _context.Versions
                  .Include(v => v.OsDetails)
                  .OrderByDescending(v => v.VersionNumber)
                  .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<AppVersion?> GetVersionByIdAsync(int id)
        {
            return await _context.Versions.Include(x => x.OsDetails).FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc/>
        public async Task<AppVersion> AddVersionAsync(AppVersion version)
        {
            _context.Versions.Add(version);
            await _context.SaveChangesAsync();
            return version;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateVersionAsync(AppVersion request)
        {
            var version = await _context.Versions.Include(x => x.OsDetails).FirstOrDefaultAsync(x => x.Id == request.Id);
            if (version == null)
            {
                return false;
            }

            version.VersionNumber = request.VersionNumber;
            version.OsDetails = request.OsDetails;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteVersionAsync(int id)
        {
            var version = await _context.Versions.Include(x => x.OsDetails).FirstOrDefaultAsync(x => x.Id == id);
            if (version == null)
            {
                return false;
            }

            _context.Versions.Remove(version);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}