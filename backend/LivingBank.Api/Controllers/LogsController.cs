using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Policy = Permissions.ViewLogs)]
public class LogsController(AppDbContext db) : ControllerBase
{
    [HttpGet("audit")]
    public async Task<ActionResult> GetAuditLogs(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = db.AuditLogs.AsQueryable();
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("errors")]
    public async Task<ActionResult> GetErrorLogs(
        [FromQuery] bool? resolved, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = db.ErrorLogs.AsQueryable();
        if (resolved.HasValue) query = query.Where(e => e.Resolved == resolved.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpPatch("errors/{id}/resolve")]
    public async Task<IActionResult> ResolveError(long id)
    {
        var error = await db.ErrorLogs.FindAsync(id);
        if (error is null) return NotFound();
        error.Resolved = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
