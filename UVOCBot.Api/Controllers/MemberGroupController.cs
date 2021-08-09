using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Dto;
using UVOCBot.Core.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberGroupController : ControllerBase
    {
        private readonly DiscordContext _context;

        public MemberGroupController(DiscordContext context)
        {
            _context = context;
        }

        // GET: api/MemberGroup/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MemberGroupDto>> GetMemberGroup(ulong id)
        {
            MemberGroup group = await _context.MemberGroups.FindAsync(id).ConfigureAwait(false);

            return group == null ? NotFound() : group.ToDto();
        }

        // GET api/MemberGroup/guildGroups/5
        [HttpGet("guildgroups/{id}")]
        public async Task<ActionResult<List<MemberGroupDto>>> GetGuildGroups(ulong id)
        {
            List<MemberGroup> groups = await _context.MemberGroups.Where(g => g.GuildId == id).ToListAsync().ConfigureAwait(false);

            return groups.ConvertAll(g => g.ToDto());
        }

        // GET api/MemberGroup/?guildId=1&groupName=""
        [HttpGet]
        public async Task<ActionResult<MemberGroupDto>> GetMemberGroup([FromQuery] ulong guildId, [FromQuery] string groupName)
        {
            MemberGroup group = await _context.MemberGroups.FirstOrDefaultAsync(g => g.GuildId == guildId && g.GroupName == groupName).ConfigureAwait(false);

            return group == default ? NotFound() : group.ToDto();
        }

        // POST api/MemberGroup
        [HttpPost]
        public async Task<ActionResult<MemberGroupDto>> Post(MemberGroupDto group)
        {
            IQueryable<MemberGroup> guildGroups = _context.MemberGroups.Where(g => g.GuildId.Equals(group.GuildId));
            if (guildGroups.Any(g => g.GroupName.Equals(group.GroupName)))
                return Conflict();

            _context.MemberGroups.Add(MemberGroup.FromDto(group));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction(nameof(GetMemberGroup), new { id = group.Id }, group);
        }

        // PUT api/MemberGroup/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(ulong id, MemberGroupDto memberGroup)
        {
            if (id != memberGroup.Id)
                return BadRequest();

            _context.Entry(MemberGroup.FromDto(memberGroup)).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) when (!MemberGroupExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE api/MemberGroup/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(ulong id)
        {
            MemberGroup group = await _context.MemberGroups.FindAsync(id).ConfigureAwait(false);
            if (group is null)
                return NotFound();

            _context.MemberGroups.Remove(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        // DELETE api/MemberGroup/?guildId=1&groupName=""
        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] ulong guildId, [FromQuery] string groupName)
        {
            MemberGroup group = _context.MemberGroups.First(g => g.GuildId == guildId && g.GroupName == groupName);
            if (group is null)
                return NotFound();

            _context.MemberGroups.Remove(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }

        private bool MemberGroupExists(ulong id)
        {
            return _context.MemberGroups.Any(e => e.Id == id);
        }
    }
}
