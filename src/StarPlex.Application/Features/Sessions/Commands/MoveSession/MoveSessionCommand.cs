using MediatR;

public class MoveSessionCommand : IRequest<bool>
{
    public Guid SessionId { get; set; }
    public Guid HallId { get; set; }
    public DateTime NewStartTime { get; set; }
}