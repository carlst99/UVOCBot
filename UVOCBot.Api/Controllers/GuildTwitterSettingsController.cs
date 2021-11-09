using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Dto;
using UVOCBot.Core.Model;

namespace UVOCBot.Api.Controllers;

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
    public async Task<ActionResult<IEnumerable<GuildTwitterSettingsDto>>> GetGuildTwitterSettings([FromQuery] bool filterByEnabled = false)
    {
        if (filterByEnabled)
            return (await _context.GuildTwitterSettings.Include(m => m.TwitterUsers).Where(s => s.IsEnabled && s.TwitterUsers.Count > 0).ToListAsync().ConfigureAwait(false)).ConvertAll(e => e.ToDto());
        else
            return (await _context.GuildTwitterSettings.Include(m => m.TwitterUsers).ToListAsync().ConfigureAwait(false)).ConvertAll(e => e.ToDto());
    }

    // GET: api/GuildTwitterSettings/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GuildTwitterSettingsDto>> GetGuildTwitterSettings(ulong id)
    {
        var guildTwitterSettings = await _context.GuildTwitterSettings.Include(e => e.TwitterUsers).FirstOrDefaultAsync(e => e.GuildId == id).ConfigureAwait(false);

        return guildTwitterSettings == default ? NotFound() : guildTwitterSettings.ToDto();
    }

    [HttpGet("exists/{id}")]
    public async Task<ActionResult<bool>> Exists(ulong id)
    {
        return await _context.GuildTwitterSettings.AnyAsync(s => s.GuildId == id).ConfigureAwait(false);
    }

    // PUT: api/GuildTwitterSettings/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutGuildTwitterSettings(ulong id, GuildTwitterSettingsDto guildTwitterSettings)
    {
        if (id != guildTwitterSettings.GuildId)
            return BadRequest();

        _context.Entry(GuildTwitterSettings.FromDto(guildTwitterSettings)).State = EntityState.Modified;

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
    public async Task<ActionResult<GuildTwitterSettingsDto>> PostGuildTwitterSettings(GuildTwitterSettingsDto guildTwitterSettings)
    {
        if (await _context.GuildTwitterSettings.AnyAsync(s => s.GuildId == guildTwitterSettings.GuildId).ConfigureAwait(false))
            return Conflict();

        _context.GuildTwitterSettings.Add(GuildTwitterSettings.FromDto(guildTwitterSettings));
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

    private bool GuildTwitterSettingsExists(ulong id)
    {
        return _context.GuildTwitterSettings.Any(e => e.GuildId == id);
    }
}
