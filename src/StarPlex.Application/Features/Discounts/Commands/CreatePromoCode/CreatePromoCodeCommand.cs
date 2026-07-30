using MediatR;

namespace StarPlex.Application.Features.Discounts.Commands.CreatePromoCode;

public class CreatePromoCodeCommand : IRequest<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int UsageLimit { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}