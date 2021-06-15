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
    public class GuildTwitterSettingsController : ControllerBase
    {
        private readonly DiscordContext _context;

        public GuildTwitterSettingsController(DiscordContext context)
        {
            _context = context;
        }

        // GET: api/GuildTwitterSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuildTwitterSettingsDTO>>> GetGuildTwitterSettings([FromQuery] bool filterByEnabled = false)
        {
            if (filterByEnabled)
                return (await _context.GuildTwitterSettings.Include(m => m.TwitterUsers).Where(s => s.IsEnabled && s.TwitterUsers.Count > 0).ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
            else
                return (await _context.GuildTwitterSettings.Include(m => m.TwitterUsers).ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
        }

        // GET: api/GuildTwitterSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GuildTwitterSettingsDTO>> GetGuildTwitterSettings(ulong id)
        {
            var guildTwitterSettings = await _context.GuildTwitterSettings.Include(e => e.TwitterUsers).FirstOrDefaultAsync(e => e.GuildId == id).ConfigureAwait(false);

            return guildTwitterSettings == default ? NotFound() : ToDTO(guildTwitterSettings);
        }

        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> Exists(ulong id)
        {
            return await _context.GuildTwitterSettings.AnyAsync(s => s.GuildId == id).ConfigureAwait(false);
        }

        // PUT: api/GuildTwitterSettings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuildTwitterSettings(ulong id, GuildTwitterSettingsDTO guildTwitterSettings)
        {
            if (id != guildTwitterSettings.GuildId)
                return BadRequest();

            _context.Entry(FromDTO(guildTwitterSettings)).State = EntityState.Modified;

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
        public async Task<ActionResult<GuildTwitterSettingsDTO>> PostGuildTwitterSettings(GuildTwitterSettingsDTO guildTwitterSettings)
        {
            if (await _context.GuildTwitterSettings.AnyAsync(s => s.GuildId == guildTwitterSettings.GuildId).ConfigureAwait(false))
                return Conflict();

            _context.GuildTwitterSettings.Add(FromDTO(guildTwitterSettings));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetGuildTwitterSettings), new { id = guildTwitterSettings.GuildId }, guildTwitterSettings);
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

        private static GuildTwitterSettingsDTO ToDTO(GuildTwitterSettings settings)
        {
            return new GuildTwitterSettingsDTO
            {
                GuildId = settings.GuildId,
                IsEnabled = settings.IsEnabled,
                RelayChannelId = settings.RelayChannelId,
                TwitterUsers = settings.TwitterUsers.Select(u => u.UserId).ToList()
            };
        }

        private static GuildTwitterSettings FromDTO(GuildTwitterSettingsDTO dto)
        {
            return new GuildTwitterSettings
            {
                GuildId = dto.GuildId,
                IsEnabled = dto.IsEnabled,
                RelayChannelId = dto.RelayChannelId
            };
        }

        private bool GuildTwitterSettingsExists(ulong id)
        {
            return _context.GuildTwitterSettings.Any(e => e.GuildId == id);
        }
    }
}
