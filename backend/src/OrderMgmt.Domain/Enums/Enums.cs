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
    ConvertedToOrder = 4,
    Cancelled = 9,
}

public enum OrderStatus
{
    New = 1,
    Preparing = 2,
    ReadyToDeliver = 3,
    Delivering = 4,
    Delivered = 5,
    Completed = 6,
    Cancelled = 9,
}

public enum PaymentStatus
{
    Unpaid = 1,
    Partial = 2,
    Paid = 3,
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
}

public enum DocumentType
{
    Quotation = 1,
    Order = 2,
    DeliveryNote = 3,
    StockOutSlip = 4,
}

public enum UserStatus
{
    Active = 1,
    Disabled = 0,
}
