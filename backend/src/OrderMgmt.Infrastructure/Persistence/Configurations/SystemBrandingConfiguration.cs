using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderMgmt.Domain.Branding;

namespace OrderMgmt.Infrastructure.Persistence.Configurations;

public class SystemBrandingConfiguration : IEntityTypeConfiguration<SystemBranding>
{
    public void Configure(EntityTypeBuilder<SystemBranding> b)
    {
        b.ToTable("system_branding");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.LogoFull).HasColumnType("bytea");
        b.Property(x => x.LogoMark).HasColumnType("bytea");
        b.Property(x => x.LogoFullContentType).HasMaxLength(64);
        b.Property(x => x.LogoMarkContentType).HasMaxLength(64);

        b.HasData(new SystemBranding
        {
            Id = 1,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        });
    }
}
