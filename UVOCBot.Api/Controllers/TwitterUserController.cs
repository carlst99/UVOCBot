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
    public class TwitterUserController : ControllerBase
    {
        private readonly DiscordContext _context;

        public TwitterUserController(DiscordContext context)
        {
            _context = context;
        }

        // GET: api/TwitterUser
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TwitterUserDTO>>> GetTwitterUsers()
        {
            return (await _context.TwitterUsers.ToListAsync().ConfigureAwait(false)).ConvertAll(e => ToDTO(e));
        }

        // GET: api/TwitterUser/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TwitterUserDTO>> GetTwitterUser(long id)
        {
            var twitterUser = await _context.TwitterUsers.FindAsync(id).ConfigureAwait(false);

            return twitterUser == null ? NotFound() : ToDTO(twitterUser);
        }

        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> Exists(long id)
        {
            return await _context.TwitterUsers.AnyAsync(u => u.UserId == id).ConfigureAwait(false);
        }

        // PUT: api/TwitterUser/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTwitterUser(long id, TwitterUserDTO twitterUser)
        {
            if (id != twitterUser.UserId)
                return BadRequest();

            _context.Entry(await FromDTO(twitterUser).ConfigureAwait(false)).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!TwitterUserExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/TwitterUser
        [HttpPost]
        public async Task<ActionResult<TwitterUserDTO>> PostTwitterUser(TwitterUserDTO twitterUser)
        {
            if (await _context.TwitterUsers.AnyAsync(t => t.UserId == twitterUser.UserId).ConfigureAwait(false))
                return Conflict();

            _context.TwitterUsers.Add(await FromDTO(twitterUser).ConfigureAwait(false));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetTwitterUser), new { id = twitterUser.UserId }, twitterUser);
        }

        // DELETE: api/TwitterUser/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTwitterUser(long id)
        {
            var twitterUser = await _context.TwitterUsers.FindAsync(id).ConfigureAwait(false);
            if (twitterUser == null)
                return NotFound();

            _context.TwitterUsers.Remove(twitterUser);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private static TwitterUserDTO ToDTO(TwitterUser user)
        {
            return new TwitterUserDTO
            {
                UserId = user.UserId,
                LastRelayedTweetId = user.LastRelayedTweetId,
                Guilds = user.Guilds.Select(g => g.GuildId).ToList()
            };
        }

        private async Task<TwitterUser> FromDTO(TwitterUserDTO dto)
        {
            TwitterUser user = new()
            {
                UserId = dto.UserId,
                LastRelayedTweetId = dto.LastRelayedTweetId
            };

            foreach (ulong id in dto.Guilds)
                user.Guilds.Add(await _context.FindAsync<GuildTwitterSettings>(id).ConfigureAwait(false));

            return user;
        }

        private bool TwitterUserExists(long id)
        {
            return _context.TwitterUsers.Any(e => e.UserId == id);
        }
    }
}
