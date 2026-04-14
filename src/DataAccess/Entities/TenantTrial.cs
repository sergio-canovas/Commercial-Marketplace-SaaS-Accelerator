using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marketplace.SaaS.Accelerator.DataAccess.Entities
{
    [Table("TenantTrial")]
    public class TenantTrial
    {
        [Key]
        public Guid TenantId { get; set; }

        public DateTime StartDateUtc { get; set; }
    }
}
