using MediatR;
using System;
using System.Collections.Generic;

namespace StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;

public class GenerateScheduleCommand : IRequest<int>
{
    public Guid CinemaId { get; set; }
    public DateTime TargetDate { get; set; }
    public decimal BasePrice { get; set; }
    public List<Guid> MovieIds { get; set; } = new();
}