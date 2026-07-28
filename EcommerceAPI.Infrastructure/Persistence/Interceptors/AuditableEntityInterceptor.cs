using EcommerceAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EcommerceAPI.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangeAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
            UpdateAuditFields(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext context)
    {
        // From Jwt Claims
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "System";

        var entries = context.ChangeTracker.Entries<BaseAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = userId;
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedBy = userId;
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}