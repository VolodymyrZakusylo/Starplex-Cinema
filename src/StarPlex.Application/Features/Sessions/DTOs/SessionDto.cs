using System;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.DTOs;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string MoviePosterUrl { get; set; } = string.Empty;
    public int MovieDurationInMinutes { get; set; }
    public Guid HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public Guid CinemaId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public SessionStatus Status { get; set; }
}