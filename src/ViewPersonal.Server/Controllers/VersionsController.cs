using Microsoft.AspNetCore.Mvc;
using ViewPersonal.Server.Models;
using ViewPersonal.Server.Models.DTOs;
using ViewPersonal.Server.Repositories.Interfaces;

namespace ViewPersonal.Server.Controllers
{
    /// <summary>
    /// API controller for managing application versions
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class VersionsController : ControllerBase
    {
        private readonly IVersionRepository _versionRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionsController"/> class
        /// </summary>
        /// <param name="versionRepository">The version repository</param>
        public VersionsController(IVersionRepository versionRepository)
        {
            _versionRepository = versionRepository;
        }

        /// <summary>
        /// Gets all versions
        /// </summary>
        /// <returns>A collection of all versions</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VersionResponseDto>>> GetVersions()
        {
            var versions = await _versionRepository.GetAllVersionsAsync();
            var versionDtos = versions.Select(v => new VersionResponseDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                ReleaseDate = v.ReleaseDate,
                OsDetails = v.OsDetails.Select(o => new VersionOsDetailsResponseDto
                {
                    Id = o.Id,
                    OS = o.OperatingSystem,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            }).ToList();
            
            return Ok(versionDtos);
        }

        /// <summary>
        /// Gets the latest version
        /// </summary>
        /// <returns>The latest version</returns>
        [HttpGet("latest")]
        public async Task<ActionResult<VersionResponseDto>> GetLatestVersion()
        {
            var version = await _versionRepository.GetLatestVersionAsync();
            if (version == null)
            {
                return NotFound("No versions found");
            }

            var versionDto = new VersionResponseDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                ReleaseDate = version.ReleaseDate,
                OsDetails = version.OsDetails.Select(o => new VersionOsDetailsResponseDto
                {
                    Id = o.Id,
                    OS = o.OperatingSystem,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            };

            return Ok(versionDto);
        }

        /// <summary>
        /// Gets a specific version by ID
        /// </summary>
        /// <param name="id">The ID of the version to get</param>
        /// <returns>The requested version</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<VersionResponseDto>> GetVersion(int id)
        {
            var version = await _versionRepository.GetVersionByIdAsync(id);
            if (version == null)
            {
                return NotFound($"Version with ID {id} not found");
            }

            var versionDto = new VersionResponseDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                ReleaseDate = version.ReleaseDate,
                OsDetails = version.OsDetails.Select(o => new VersionOsDetailsResponseDto
                {
                    Id = o.Id,
                    OS = o.OperatingSystem,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            };

            return Ok(versionDto);
        }

        /// <summary>
        /// Creates a new version
        /// </summary>
        /// <param name="versionDto">The version to create</param>
        /// <returns>The created version</returns>
        [HttpPost]
        public async Task<ActionResult<VersionResponseDto>> CreateVersion(VersionDto versionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var version = new AppVersion
            {
                VersionNumber = versionDto.VersionNumber,
                ReleaseDate = DateTime.UtcNow,
                OsDetails = versionDto.OsDetails.Select(o => new VersionOsDetails
                {
                    OperatingSystem = o.OS,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            };

            var createdVersion = await _versionRepository.AddVersionAsync(version);
            
            var responseDto = new VersionResponseDto
            {
                Id = createdVersion.Id,
                VersionNumber = createdVersion.VersionNumber,
                ReleaseDate = createdVersion.ReleaseDate,
                OsDetails = createdVersion.OsDetails.Select(o => new VersionOsDetailsResponseDto
                {
                    Id = o.Id,
                    OS = o.OperatingSystem,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            };

            return CreatedAtAction(nameof(GetVersion), new { id = createdVersion.Id }, responseDto);
        }

        /// <summary>
        /// Updates an existing version
        /// </summary>
        /// <param name="id">The ID of the version to update</param>
        /// <param name="versionDto">The updated version data</param>
        /// <returns>No content if successful</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVersion(int id, VersionDto versionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var version = new AppVersion
            {
                Id = id,
                VersionNumber = versionDto.VersionNumber,
                OsDetails = versionDto.OsDetails.Select(o => new VersionOsDetails
                {
                    OperatingSystem = o.OS,
                    DownloadUrl = o.DownloadUrl
                }).ToList()
            };

            var success = await _versionRepository.UpdateVersionAsync(version);
            if (!success)
            {
                return NotFound($"Version with ID {id} not found");
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a version
        /// </summary>
        /// <param name="id">The ID of the version to delete</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVersion(int id)
        {
            var success = await _versionRepository.DeleteVersionAsync(id);
            if (!success)
            {
                return NotFound($"Version with ID {id} not found");
            }

            return NoContent();
        }
    }
}