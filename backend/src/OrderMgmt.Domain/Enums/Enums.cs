namespace OrderMgmt.Domain.Enums;

public enum CustomerStatus
{
    Active = 1,
    Inactive = 0,
}

public enum CustomerGroup
{
    Company = 1,
    Agent = 2,
    Retail = 3,
    Project = 4,
}

public enum ProductStatus
{
    Active = 1,
    Inactive = 0,
}

public enum PricingMode
{
    PerUnit = 1,
    PerSquareMeter = 2,
    PerLinearMeter = 3,
    PerCubicMeter = 4,
}

public enum QuotationStatus
{
    Draft = 1,
    Sent = 2,
    Confirmed = 3,
    Cancelled = 9,
}

public enum UserStatus
{
    Active = 1,
    Disabled = 0,
}
