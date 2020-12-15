using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBotApi.Model;

namespace UVOCBotApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuildTwitterSettingsController : ControllerBase
    {
        private readonly BotContext _context;

        public GuildTwitterSettingsController(BotContext context)
        {
            _context = context;
        }

        // GET: api/GuildTwitterSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuildTwitterSettings>>> GetGuildTwitterSettings()
        {
            return await _context.GuildTwitterSettings.ToListAsync().ConfigureAwait(false);
        }

        // GET: api/GuildTwitterSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GuildTwitterSettings>> GetGuildTwitterSettings(ulong id)
        {
            var guildTwitterSettings = await _context.GuildTwitterSettings.FindAsync(id).ConfigureAwait(false);

            return guildTwitterSettings ?? (ActionResult<GuildTwitterSettings>)NotFound();
        }

        // PUT: api/GuildTwitterSettings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuildTwitterSettings(ulong id, GuildTwitterSettings guildTwitterSettings)
        {
            if (id != guildTwitterSettings.GuildId)
                return BadRequest();

            _context.Entry(guildTwitterSettings).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!GuildTwitterSettingsExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/GuildTwitterSettings
        [HttpPost]
        public async Task<ActionResult<GuildTwitterSettings>> PostGuildTwitterSettings(GuildTwitterSettings guildTwitterSettings)
        {
            _context.GuildTwitterSettings.Add(guildTwitterSettings);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction("GetGuildTwitterSettings", new { id = guildTwitterSettings.GuildId }, guildTwitterSettings);
        }

        // DELETE: api/GuildTwitterSettings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuildTwitterSettings(ulong id)
        {
            var guildTwitterSettings = await _context.GuildTwitterSettings.FindAsync(id).ConfigureAwait(false);
            if (guildTwitterSettings == null)
                return NotFound();

            _context.GuildTwitterSettings.Remove(guildTwitterSettings);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private bool GuildTwitterSettingsExists(ulong id)
        {
            return _context.GuildTwitterSettings.Any(e => e.GuildId == id);
        }
    }
}
