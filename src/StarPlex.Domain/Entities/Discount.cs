using System;
using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class Discount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public int UsageLimit { get; set; }
    public int UsageCount { get; set; }
}