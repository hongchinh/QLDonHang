using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> b)
    {
        b.ToTable("push_subscriptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Endpoint).IsRequired().HasMaxLength(2048);
        b.Property(x => x.P256DH).IsRequired().HasMaxLength(512);
        b.Property(x => x.Auth).IsRequired().HasMaxLength(256);
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");

        b.HasIndex(x => x.Endpoint).IsUnique()
            .HasDatabaseName("ix_push_subscriptions_endpoint");
        b.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_push_subscriptions_user_id");
    }
}
