using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using Marketplace.SaaS.Accelerator.Services.Models;
using Marketplace.SaaS.Accelerator.Services.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace Marketplace.SaaS.Accelerator.Services.Test;

[TestClass]
public class TenantEntitlementServiceTest
{
    [TestMethod]
    public void ReturnsActiveWhenTenantHasActiveSubscribedSubscription()
    {
        var tenantId = Guid.NewGuid();
        var repository = CreateRepository(new[]
        {
            CreateSubscription(tenantId, SubscriptionStatusEnum.Subscribed.ToString(), true, "pro")
        });

        var trialRepo = new Mock<Marketplace.SaaS.Accelerator.DataAccess.Contracts.ITenantTrialRepository>(); trialRepo.Setup(r => r.GetByTenantId(It.IsAny<Guid>())).Returns(new Marketplace.SaaS.Accelerator.DataAccess.Entities.TenantTrial { StartDateUtc = DateTime.UtcNow }); var service = new TenantEntitlementService(repository.Object, trialRepo.Object);

        var result = service.GetByTenantId(tenantId);

        Assert.AreEqual("active", result.Status);
        Assert.AreEqual("pro", result.PlanId);
    }

    [TestMethod]
    public void ReturnsPendingWhenTenantHasPendingActivationAndNoActiveSubscription()
    {
        var tenantId = Guid.NewGuid();
        var repository = CreateRepository(new[]
        {
            CreateSubscription(tenantId, SubscriptionStatusEnum.PendingActivation.ToString(), true, "trial")
        });

        var trialRepo = new Mock<Marketplace.SaaS.Accelerator.DataAccess.Contracts.ITenantTrialRepository>(); trialRepo.Setup(r => r.GetByTenantId(It.IsAny<Guid>())).Returns(new Marketplace.SaaS.Accelerator.DataAccess.Entities.TenantTrial { StartDateUtc = DateTime.UtcNow }); var service = new TenantEntitlementService(repository.Object, trialRepo.Object);

        var result = service.GetByTenantId(tenantId);

        Assert.AreEqual("pending", result.Status);
        Assert.AreEqual("trial", result.PlanId);
    }

    [TestMethod]
    public void PrefersActiveSubscriptionOverNewerPendingSubscription()
    {
        var tenantId = Guid.NewGuid();
        var olderActive = CreateSubscription(tenantId, SubscriptionStatusEnum.Subscribed.ToString(), true, "paid");
        olderActive.ModifyDate = DateTime.UtcNow.AddHours(-2);

        var newerPending = CreateSubscription(tenantId, SubscriptionStatusEnum.PendingActivation.ToString(), true, "pending");
        newerPending.ModifyDate = DateTime.UtcNow;

        var repository = CreateRepository(new[] { newerPending, olderActive });
        var trialRepo = new Mock<Marketplace.SaaS.Accelerator.DataAccess.Contracts.ITenantTrialRepository>(); trialRepo.Setup(r => r.GetByTenantId(It.IsAny<Guid>())).Returns(new Marketplace.SaaS.Accelerator.DataAccess.Entities.TenantTrial { StartDateUtc = DateTime.UtcNow }); var service = new TenantEntitlementService(repository.Object, trialRepo.Object);

        var result = service.GetByTenantId(tenantId);

        Assert.AreEqual("active", result.Status);
        Assert.AreEqual("paid", result.PlanId);
    }

    [TestMethod]
    public void ReturnsInactiveWhenTenantIsUnknown()
    {
        var repository = CreateRepository(Array.Empty<Subscriptions>());
        var trialRepo = new Mock<Marketplace.SaaS.Accelerator.DataAccess.Contracts.ITenantTrialRepository>();
        trialRepo.Setup(r => r.GetByTenantId(It.IsAny<Guid>())).Returns((Marketplace.SaaS.Accelerator.DataAccess.Entities.TenantTrial)null);
        var service = new TenantEntitlementService(repository.Object, trialRepo.Object);

        var result = service.GetByTenantId(Guid.NewGuid());

        Assert.AreEqual("inactive", result.Status);
        Assert.IsNull(result.PlanId);
        Assert.IsNull(result.ExpiresOn);
    }

    private static Mock<ISubscriptionsRepository> CreateRepository(IEnumerable<Subscriptions> subscriptions)
    {
        var repository = new Mock<ISubscriptionsRepository>();
        repository.Setup(x => x.GetSubscriptionsByPurchaserTenantId(It.IsAny<Guid>()))
            .Returns<Guid>(tenantId =>
            {
                var matches = new List<Subscriptions>();
                foreach (var subscription in subscriptions)
                {
                    if (subscription.PurchaserTenantId == tenantId)
                    {
                        matches.Add(subscription);
                    }
                }

                return matches;
            });

        return repository;
    }

    private static Subscriptions CreateSubscription(Guid tenantId, string status, bool isActive, string planId)
    {
        return new Subscriptions
        {
            AmpsubscriptionId = Guid.NewGuid(),
            PurchaserTenantId = tenantId,
            SubscriptionStatus = status,
            IsActive = isActive,
            AmpplanId = planId,
            EndDate = DateTime.UtcNow.AddMonths(1),
            CreateDate = DateTime.UtcNow.AddDays(-1),
            ModifyDate = DateTime.UtcNow.AddMinutes(-30)
        };
    }
}
