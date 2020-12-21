using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Api.Model;

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
        public async Task<ActionResult<IEnumerable<GuildSettings>>> GetGuildSettings()
        {
            return await _context.GuildSettings.ToListAsync().ConfigureAwait(false);
        }

        // GET: api/GuildSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GuildSettings>> GetGuildSettings(ulong id)
        {
            var guildSettings = await _context.GuildSettings.FindAsync(id).ConfigureAwait(false);

            return guildSettings ?? (ActionResult<GuildSettings>)NotFound();
        }

        // PUT: api/GuildSettings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuildSettings(ulong id, GuildSettings guildSettings)
        {
            if (id != guildSettings.GuildId)
                return BadRequest();

            _context.Entry(guildSettings).State = EntityState.Modified;

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
        public async Task<ActionResult<GuildSettings>> PostGuildSettings(GuildSettings guildSettings)
        {
            _context.GuildSettings.Add(guildSettings);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction("GetGuildSettings", new { id = guildSettings.GuildId }, guildSettings);
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

        private bool GuildSettingsExists(ulong id)
        {
            return _context.GuildSettings.Any(e => e.GuildId == id);
        }
    }
}
