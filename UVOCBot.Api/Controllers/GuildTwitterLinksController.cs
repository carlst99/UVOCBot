using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UVOCBot.Api.Model;

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuildTwitterLinksController : ControllerBase
    {
        private readonly BotContext _context;

        public GuildTwitterLinksController(BotContext context)
        {
            _context = context;
        }

        // POST: api/GuildTwitterLinks
        [HttpPost]
        public async Task<ActionResult> CreateLink(ulong guildTwitterSettingsId, long twitterUserId)
        {
            GuildTwitterSettings settings = await _context.FindAsync<GuildTwitterSettings>(guildTwitterSettingsId).ConfigureAwait(false);
            if (settings is null)
                return BadRequest();

            TwitterUser user = await _context.FindAsync<TwitterUser>(twitterUserId).ConfigureAwait(false);
            if (user is null)
                return BadRequest();

            if (!settings.TwitterUsers.Contains(user))
                settings.TwitterUsers.Add(user);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        // DELETE: api/GuildTwitterLinks
        [HttpDelete]
        public async Task<IActionResult> DeleteLink(ulong guildTwitterSettingsId, long twitterUserId)
        {
            GuildTwitterSettings settings = await _context.FindAsync<GuildTwitterSettings>(guildTwitterSettingsId).ConfigureAwait(false);
            if (settings is null)
                return BadRequest();

            TwitterUser user = await _context.FindAsync<TwitterUser>(twitterUserId).ConfigureAwait(false);
            if (user is null)
                return BadRequest();

            if (settings.TwitterUsers.Contains(user))
                settings.TwitterUsers.Remove(user);
            else
                return BadRequest();

            await _context.SaveChangesAsync().ConfigureAwait(false);
            return NoContent();
        }
    }
}
