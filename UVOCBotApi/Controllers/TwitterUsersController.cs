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
    public class TwitterUsersController : ControllerBase
    {
        private readonly BotContext _context;

        public TwitterUsersController(BotContext context)
        {
            _context = context;
        }

        // GET: api/TwitterUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TwitterUser>>> GetTwitterUsers()
        {
            return await _context.TwitterUsers.ToListAsync().ConfigureAwait(false);
        }

        // GET: api/TwitterUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TwitterUser>> GetTwitterUser(long id)
        {
            var twitterUser = await _context.TwitterUsers.FindAsync(id).ConfigureAwait(false);

            return twitterUser ?? (ActionResult<TwitterUser>)NotFound();
        }

        // PUT: api/TwitterUsers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTwitterUser(long id, TwitterUser twitterUser)
        {
            if (id != twitterUser.UserId)
                return BadRequest();

            _context.Entry(twitterUser).State = EntityState.Modified;

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

        // POST: api/TwitterUsers
        [HttpPost]
        public async Task<ActionResult<TwitterUser>> PostTwitterUser(TwitterUser twitterUser)
        {
            _context.TwitterUsers.Add(twitterUser);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetTwitterUser), new { id = twitterUser.UserId }, twitterUser);
        }

        // DELETE: api/TwitterUsers/5
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

        private bool TwitterUserExists(long id)
        {
            return _context.TwitterUsers.Any(e => e.UserId == id);
        }
    }
}
