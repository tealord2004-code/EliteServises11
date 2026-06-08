using EliteJobs.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace EliteJobs.App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DirectoryController : ControllerBase
    {
        private readonly IDirectoryService _directoryService;

        public DirectoryController(IDirectoryService directoryService)
        {
            _directoryService = directoryService;
        }

        /// <summary>
        /// Поиск городов
        /// </summary>
        [HttpGet("cities")]
        public async Task<IActionResult> SearchCities([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
                return Ok(new List<object>());

            var results = await _directoryService.SearchCitiesAsync(q, 10);
            return Ok(results.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                group = r.Group.Name,
                description = r.Description
            }));
        }

        /// <summary>
        /// Получение всех групп городов с элементами
        /// </summary>
        [HttpGet("cities/all")]
        public async Task<IActionResult> GetAllCities()
        {
            var groups = await _directoryService.GetCityGroupsAsync();
            return Ok(groups.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                items = g.Items.Select(i => new { id = i.Id, name = i.Name, description = i.Description })
            }));
        }

        /// <summary>
        /// Поиск профессий
        /// </summary>
        [HttpGet("professions")]
        public async Task<IActionResult> SearchProfessions([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
                return Ok(new List<object>());

            var results = await _directoryService.SearchProfessionsAsync(q, 10);
            return Ok(results.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                group = r.Group.Name
            }));
        }

        /// <summary>
        /// Получение всех групп профессий с элементами
        /// </summary>
        [HttpGet("professions/all")]
        public async Task<IActionResult> GetAllProfessions()
        {
            var groups = await _directoryService.GetProfessionGroupsAsync();
            return Ok(groups.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                items = g.Items.Select(i => new { id = i.Id, name = i.Name })
            }));
        }
    }
}