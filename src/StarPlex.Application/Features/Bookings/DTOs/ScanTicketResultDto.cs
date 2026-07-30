namespace StarPlex.Application.Features.Bookings.Commands.ScanTicket;

public class ScanTicketResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MovieTitle { get; set; }
    public string? HallName { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public string? StartTime { get; set; }
}