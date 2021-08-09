using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Dto;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanetsideSettingsController : ControllerBase
    {
        private readonly DiscordContext _context;

        public PlanetsideSettingsController(DiscordContext context)
        {
            _context = context;
        }

        // GET: api/GuildSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlanetsideSettingsDto>>> GetPlanetsideSettings()
        {
            return (await _context.PlanetsideSettings.ToListAsync().ConfigureAwait(false)).ConvertAll(e => e.ToDto());
        }

        // GET: api/PlanetsideSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlanetsideSettingsDto>> GetPlanetsideSettings(ulong id)
        {
            var planetsideSettings = await _context.PlanetsideSettings.FindAsync(id).ConfigureAwait(false);

            return planetsideSettings == null ? NotFound() : planetsideSettings.ToDto();
        }

        // PUT: api/PlanetsideSettings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlanetsideSettings(ulong id, PlanetsideSettingsDto planetsideSettings)
        {
            if (id != planetsideSettings.GuildId)
                return BadRequest();

            _context.Entry(PlanetsideSettings.FromDto(planetsideSettings)).State = EntityState.Modified;

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
        public async Task<ActionResult<PlanetsideSettingsDto>> PostPlanetsideSettings(PlanetsideSettingsDto planetsideSettings)
        {
            if (await _context.PlanetsideSettings.AnyAsync((s) => s.GuildId == planetsideSettings.GuildId).ConfigureAwait(false))
                return Conflict();

            _context.PlanetsideSettings.Add(PlanetsideSettings.FromDto(planetsideSettings));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetPlanetsideSettings), new { id = planetsideSettings.GuildId }, planetsideSettings);
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

        private bool PlanetsideSettingsExists(ulong id)
        {
            return _context.PlanetsideSettings.Any(e => e.GuildId == id);
        }
    }
}
