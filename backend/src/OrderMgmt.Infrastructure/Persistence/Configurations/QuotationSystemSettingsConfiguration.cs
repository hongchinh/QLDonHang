using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderMgmt.Domain.Entities.Sales;

namespace OrderMgmt.Infrastructure.Persistence.Configurations;

public class QuotationSystemSettingsConfiguration : IEntityTypeConfiguration<QuotationSystemSettings>
{
    public void Configure(EntityTypeBuilder<QuotationSystemSettings> b)
    {
        b.ToTable("quotation_system_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.RevenueReportingDateField).HasMaxLength(50).IsRequired();

        b.HasData(new QuotationSystemSettings
        {
            Id = 1,
            RevenueReportingDateField = "QuotationDate",
            UpdatedAt = DateTimeOffset.UnixEpoch,
        });
    }
}
