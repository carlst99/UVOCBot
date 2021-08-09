using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Dto;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuildWelcomeMessageController : ControllerBase
    {
        private readonly DiscordContext _context;

        public GuildWelcomeMessageController(DiscordContext context)
        {
            _context = context;
        }

        // GET: api/GuildWelcomeMessage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GuildWelcomeMessageDto>> GetGuildWelcomeMessage(ulong id)
        {
            GuildWelcomeMessage welcomeMessage = await _context.GuildWelcomeMessages.FindAsync(id).ConfigureAwait(false);

            return welcomeMessage == default ? NotFound() : welcomeMessage.ToDto();
        }

        // PUT: api/GuildWelcomeMessage/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuildWelcomeMessage(ulong id, GuildWelcomeMessageDto guildWelcomeMessageDto)
        {
            if (id != guildWelcomeMessageDto.GuildId)
                return BadRequest();

            _context.Entry(GuildWelcomeMessage.FromDto(guildWelcomeMessageDto)).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!WelcomeMessageExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/GuildSettings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GuildSettingsDto>> PostGuildWelcomeMessage(GuildWelcomeMessageDto welcomeMessage)
        {
            if (await _context.GuildWelcomeMessages.AnyAsync(s => s.GuildId == welcomeMessage.GuildId).ConfigureAwait(false))
                return Conflict();

            _context.GuildWelcomeMessages.Add(GuildWelcomeMessage.FromDto(welcomeMessage));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetGuildWelcomeMessage), new { id = welcomeMessage.GuildId }, welcomeMessage);
        }

        // DELETE: api/GuildWelcomeMessage/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuildWelcomeMessage(ulong id)
        {
            GuildWelcomeMessage welcomeMessage = await _context.GuildWelcomeMessages.FindAsync(id).ConfigureAwait(false);
            if (welcomeMessage == null)
                return NotFound();

            _context.GuildWelcomeMessages.Remove(welcomeMessage);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private bool WelcomeMessageExists(ulong id)
        {
            return _context.GuildWelcomeMessages.Any(e => e.GuildId == id);
        }
    }
}
