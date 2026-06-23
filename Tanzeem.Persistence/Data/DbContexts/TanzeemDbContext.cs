using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Persistence.Data.DbContexts {
    public class TanzeemDbContext : DbContext {
        private readonly ICurrentService currentService;
        private readonly IHttpContextAccessor httpContextAccessor;

        private static readonly HashSet<string> SensitiveProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "Password",
            "SecurityStamp",
            "ConcurrencyStamp",
            "TwoFactorSecret",
            "RefreshToken",
            "AccessToken",
            "NormalizedEmail",
            "NormalizedUserName"
        };

        public TanzeemDbContext(DbContextOptions<TanzeemDbContext> options, ICurrentService _currentService,IHttpContextAccessor _httpContextAccessor) : base(options) {
            currentService = _currentService;
            httpContextAccessor = _httpContextAccessor;
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryBatch> InventoryBatches { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<AlertConfigurations> AlertConfigurations { get; set; }
        public DbSet<DeliveryIssue> DeliveryIssues { get; set; }
        public DbSet<DeliveryIssueItem> DeliveryIssueItems { get; set; }
        public DbSet<DemandForecast> DemandForecasts { get; set; }
        public DbSet<AIConfigurations> AIConfigurations { get; set; }
        public DbSet<AuditTrial> AuditTrials { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentTime = DateTime.UtcNow;
            var userId = currentService.UserId;
            var branchId = currentService.BranchId;

            var auditEntries = new List<(AuditTrial audit, EntityEntry entry)>();

            var entries = ChangeTracker.Entries().Where(e => e is
            {
                Entity: IAuditable,
                State: EntityState.Added or EntityState.Modified or EntityState.Deleted,
            }).ToList();

            var deletedBranchIds = ChangeTracker.Entries<Branch>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(e => e.Entity.Id)
                .ToHashSet();

            foreach (var entry in entries)
            {
                var audit = CreateAuditTrialFromEntry(entry, currentTime, userId,branchId);
                if (audit.BranchId.HasValue && deletedBranchIds.Contains(audit.BranchId.Value))
                    audit.BranchId = null;

                auditEntries.Add((audit,entry));
            }

            if (!auditEntries.Any())
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            await AuditTrials.AddRangeAsync(auditEntries.Select(x => x.audit), cancellationToken);

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var (audit,entry) in auditEntries.Where(x => x.audit.Action == "Insert"))
            {
                var pkProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pkProperty?.CurrentValue != null)
                {
                    audit.EntityPrimaryKey = int.TryParse(pkProperty.CurrentValue.ToString(), out var pk) 
                        ? pk : throw new ArgumentException("primary key needs to be present");
                }
            }
            if (auditEntries.Any(x => x.audit.Action == "Insert"))
            {
                await base.SaveChangesAsync(cancellationToken);
            }
            return result;
        }

        private AuditTrial CreateAuditTrialFromEntry(EntityEntry entry, DateTime currentTime, int? userId, int? branchId)
        {
            var auditTrial = new AuditTrial()
            {
                EntityName = entry.Entity.GetType().Name,
                Action = GetAction(entry.State),
                CreatedAt = currentTime,
                UserId = userId,
                BranchId = branchId
            };
            var OldValues = new Dictionary<string, object>();
            var NewValues = new Dictionary<string, object>();

            foreach (var property in entry.Properties.Where(x => !x.IsTemporary))
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    auditTrial.EntityPrimaryKey = int.TryParse(property.CurrentValue?.ToString(), out var pk)
                        ? pk : 0;
                    continue;
                }
                if (!ShouldAuditProperty(property))
                {
                    continue;
                }
                AddPropertyValuesBasedOnState(entry.State, property, OldValues, NewValues);
            }

            auditTrial.OldValue = OldValues.Any() ? JsonSerializer.Serialize(OldValues) : null;
            auditTrial.NewValue = NewValues.Any() ? JsonSerializer.Serialize(NewValues) : null;

            return auditTrial;
        }

        private static void AddPropertyValuesBasedOnState(EntityState state, PropertyEntry property, Dictionary<string, object> oldValues, Dictionary<string, object> newValues)
        {
            var propertyName = property.Metadata.Name;
            
            switch (state)
            {
                case EntityState.Added:
                    newValues[propertyName] = property.CurrentValue!;
                    break;
                
                case EntityState.Modified when property.IsModified:
                    oldValues[propertyName] = property.OriginalValue!;
                    newValues[propertyName] = property.CurrentValue!;
                    break;

                case EntityState.Deleted:
                    oldValues[propertyName] = property.OriginalValue!;
                    break;

            }
        }

        private static bool ShouldAuditProperty(PropertyEntry entry)
        {
            return !SensitiveProperties.Contains(entry.Metadata.Name);
        }

        private string GetAction(EntityState state)
            => state switch
            {
                EntityState.Added => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "UnKnown"
            };
        

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            ApplyAllGlobal(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void ApplyAllGlobal(ModelBuilder modelBuilder) {

            //Company children
            modelBuilder.Entity<Product>().HasQueryFilter(
                p => p.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            modelBuilder.Entity<TransactionItem>().HasQueryFilter(
                ti => ti.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            modelBuilder.Entity<OrderItem>().HasQueryFilter(
                oi => oi.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            modelBuilder.Entity<DeliveryIssueItem>().HasQueryFilter(
                dii => dii.OrderItem.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            modelBuilder.Entity<DemandForecast>().HasQueryFilter(
                df => df.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null);

            //modelBuilder.Entity<Supplier>().HasQueryFilter(s => s.CompanyId == currentService.CompanyId);

            //
            // Branch children
            modelBuilder.Entity<Inventory>().HasQueryFilter(i => (i.BranchId == currentService.BranchId)
            && (i.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null));

            modelBuilder.Entity<InventoryBatch>().HasQueryFilter(i => (i.BranchId == currentService.BranchId)
            && (i.Product.CompanyId == currentService.CompanyId || currentService.CompanyId == null));

            /*
            modelBuilder.Entity<Transaction>().HasQueryFilter(t => t.BranchId == currentService.BranchId);
            modelBuilder.Entity<Order>().HasQueryFilter(o => o.BranchId == currentService.BranchId);
            modelBuilder.Entity<Notification>().HasQueryFilter(n => n.BranchId == currentService.BranchId);
            */

            //create for alert configuration
        }

    }
}
