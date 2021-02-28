using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Api.Model;
using UVOCBot.Core.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UVOCBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberGroupController : ControllerBase
    {
        private readonly BotContext _context;

        public MemberGroupController(BotContext context)
        {
            _context = context;
        }

        // GET: api/MemberGroup/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MemberGroupDTO>> Get(ulong id)
        {
            return ToDTO(await _context.MemberGroups.FindAsync(id).ConfigureAwait(false));
        }

        // GET api/MemberGroup/?guildId=1&groupName=""
        [HttpGet]
        public ActionResult<MemberGroupDTO> Get([FromQuery] ulong guildId, [FromQuery] string groupName)
        {
            return ToDTO(_context.MemberGroups.First(g => g.GuildId == guildId && g.GroupName == groupName));
        }

        // POST api/MemberGroup
        [HttpPost]
        public async Task<ActionResult<MemberGroupDTO>> Post(MemberGroupDTO memberGroup)
        {
            IQueryable<MemberGroup> guildGroups = _context.MemberGroups.Where(g => g.GuildId.Equals(memberGroup.GuildId));
            if (guildGroups.Any(g => g.GroupName.Equals(memberGroup.GroupName)))
                return Conflict();

            _context.MemberGroups.Add(FromDTO(memberGroup));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction("GetMemberGroup", new { id = memberGroup.Id }, memberGroup);
        }

        // PUT api/MemberGroup/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(ulong id, MemberGroupDTO memberGroup)
        {
            if (id != memberGroup.Id)
                return BadRequest();

            _context.Entry(FromDTO(memberGroup)).State = EntityState.Modified;

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
            MemberGroup memberGroup = await _context.MemberGroups.FindAsync(id).ConfigureAwait(false);
            if (memberGroup == null)
                return NotFound();

            _context.MemberGroups.Remove(memberGroup);
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

        public MemberGroupDTO ToDTO(MemberGroup group)
        {
            return new MemberGroupDTO
            {
                Id = group.Id,
                GuildId = group.GuildId,
                CreatedAt = group.CreatedAt,
                GroupName = group.GroupName,
                UserIds = new List<ulong>(group.UserIds.Split('\n').Select(s => ulong.Parse(s)))
            };
        }

        public MemberGroup FromDTO(MemberGroupDTO dto)
        {
            return new MemberGroup
            {
                Id = dto.Id,
                GuildId = dto.GuildId,
                CreatedAt = dto.CreatedAt,
                GroupName = dto.GroupName,
                UserIds = string.Join('\n', dto.UserIds.Select(i => i.ToString()))
            };
        }

        private bool MemberGroupExists(ulong id)
        {
            return _context.MemberGroups.Any(e => e.Id == id);
        }
    }
}
