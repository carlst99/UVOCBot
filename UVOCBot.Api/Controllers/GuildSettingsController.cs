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
    public class GuildSettingsController : ControllerBase
    {
        private readonly BotContext _context;

        public GuildSettingsController(BotContext context)
        {
            _context = context;
        }

        // GET: api/GuildSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuildSettingsDTO>>> GetGuildSettings([FromQuery] bool hasPrefix)
        {
            if (hasPrefix)
                return (await _context.GuildSettings.Where(s => !string.IsNullOrEmpty(s.Prefix)).ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
            else
                return (await _context.GuildSettings.ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
        }

        // GET: api/GuildSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GuildSettingsDTO>> GetGuildSettings(ulong id)
        {
            var guildSettings = await _context.GuildSettings.FindAsync(id).ConfigureAwait(false);

            return guildSettings == default ? NotFound() : ToDTO(guildSettings);
        }

        // PUT: api/GuildSettings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuildSettings(ulong id, GuildSettingsDTO guildSettings)
        {
            if (id != guildSettings.GuildId)
                return BadRequest();

            _context.Entry(FromDTO(guildSettings)).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!GuildSettingsExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/GuildSettings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GuildSettingsDTO>> PostGuildSettings(GuildSettingsDTO guildSettings)
        {
            if (await _context.GuildSettings.AnyAsync(s => s.GuildId == guildSettings.GuildId).ConfigureAwait(false))
                return Conflict();

            _context.GuildSettings.Add(FromDTO(guildSettings));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetGuildSettings), new { id = guildSettings.GuildId }, guildSettings);
        }

        // DELETE: api/GuildSettings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuildSettings(ulong id)
        {
            var guildSettings = await _context.GuildSettings.FindAsync(id).ConfigureAwait(false);
            if (guildSettings == null)
                return NotFound();

            _context.GuildSettings.Remove(guildSettings);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private static GuildSettingsDTO ToDTO(GuildSettings settings)
        {
            return new GuildSettingsDTO
            {
                GuildId = settings.GuildId,
                BonkChannelId = settings.BonkChannelId,
                Prefix = settings.Prefix
            };
        }

        private static GuildSettings FromDTO(GuildSettingsDTO dto)
        {
            return new GuildSettings
            {
                GuildId = dto.GuildId,
                BonkChannelId = dto.BonkChannelId,
                Prefix = dto.Prefix
            };
        }

        private bool GuildSettingsExists(ulong id)
        {
            return _context.GuildSettings.Any(e => e.GuildId == id);
        }
    }
}
