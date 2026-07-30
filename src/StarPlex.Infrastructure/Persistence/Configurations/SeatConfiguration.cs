using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Row).IsRequired().HasMaxLength(5);
        builder.Property(s => s.Type).IsRequired();
        builder.HasIndex(s => new { s.HallId, s.Row, s.Number }).IsUnique();
    }
}