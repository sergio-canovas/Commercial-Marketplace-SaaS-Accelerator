using System;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;

namespace Marketplace.SaaS.Accelerator.DataAccess.Contracts;

public interface ITenantTrialRepository
{
    TenantTrial GetByTenantId(Guid tenantId);
    void CreateTrial(Guid tenantId);
}