using System;
using System.Linq;
using Marketplace.SaaS.Accelerator.DataAccess.Context;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;

namespace Marketplace.SaaS.Accelerator.DataAccess.Services;

public class TenantTrialRepository : ITenantTrialRepository
{
    private readonly SaasKitContext context;

    public TenantTrialRepository(SaasKitContext context)
    {
        this.context = context;
    }

    public TenantTrial GetByTenantId(Guid tenantId)
    {
        return this.context.TenantTrials.FirstOrDefault(t => t.TenantId == tenantId);
    }

    public void CreateTrial(Guid tenantId)
    {
        if (GetByTenantId(tenantId) == null)
        {
            this.context.TenantTrials.Add(new TenantTrial
            {
                TenantId = tenantId,
                StartDateUtc = DateTime.UtcNow
            });
            this.context.SaveChanges();
        }
    }
}