namespace StarPlex.Domain.Enums;

public enum ScanTicketResult
{
    Success,
    InvalidStatus,
    AlreadyScanned,
    TooEarly,
    Expired
}
