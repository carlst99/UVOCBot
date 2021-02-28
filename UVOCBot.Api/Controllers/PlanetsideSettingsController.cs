using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Api.Model;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanetsideSettingsController : ControllerBase
    {
        private readonly BotContext _context;

        public PlanetsideSettingsController(BotContext context)
        {
            _context = context;
        }

        // GET: api/GuildSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlanetsideSettingsDTO>>> GetPlanetsideSettings()
        {
            return (await _context.PlanetsideSettings.ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
        }

        // GET: api/PlanetsideSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlanetsideSettingsDTO>> GetPlanetsideSettings(ulong id)
        {
            var planetsideSettings = await _context.PlanetsideSettings.FindAsync(id).ConfigureAwait(false);

            return planetsideSettings == null ? NotFound() : ToDTO(planetsideSettings);
        }

        // PUT: api/PlanetsideSettings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlanetsideSettings(ulong id, PlanetsideSettingsDTO planetsideSettings)
        {
            if (id != planetsideSettings.GuildId)
                return BadRequest();

            _context.Entry(FromDTO(planetsideSettings)).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!PlanetsideSettingsExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/PlanetsideSettings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PlanetsideSettingsDTO>> PostPlanetsideSettings(PlanetsideSettingsDTO planetsideSettings)
        {
            _context.PlanetsideSettings.Add(FromDTO(planetsideSettings));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction("GetPlanetsideSettings", new { id = planetsideSettings.GuildId }, planetsideSettings);
        }

        // DELETE: api/PlanetsideSettings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlanetsideSettings(ulong id)
        {
            var planetsideSettings = await _context.PlanetsideSettings.FindAsync(id).ConfigureAwait(false);
            if (planetsideSettings == null)
                return NotFound();

            _context.PlanetsideSettings.Remove(planetsideSettings);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private static PlanetsideSettingsDTO ToDTO(PlanetsideSettings settings)
        {
            return new PlanetsideSettingsDTO
            {
                GuildId = settings.GuildId,
                DefaultWorld = settings.DefaultWorld
            };
        }

        private static PlanetsideSettings FromDTO(PlanetsideSettingsDTO dto)
        {
            return new PlanetsideSettings
            {
                GuildId = dto.GuildId,
                DefaultWorld = dto.DefaultWorld
            };
        }

        private bool PlanetsideSettingsExists(ulong id)
        {
            return _context.PlanetsideSettings.Any(e => e.GuildId == id);
        }
    }
}
