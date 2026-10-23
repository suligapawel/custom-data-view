using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class ViewRowConfiguration : IEntityTypeConfiguration<ViewRow>
{
    public void Configure(EntityTypeBuilder<ViewRow> builder)
    {
        builder.ToTable("views");
        builder.HasKey(view => view.Id).HasName("pk_views");
        builder.Property(view => view.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(view => view.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(view => view.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(view => view.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.HasIndex(view => new { view.CreatedAt, view.Id }).HasDatabaseName("ix_views_created_at_id");
        builder.HasMany(view => view.Tiles)
            .WithOne()
            .HasForeignKey(tile => tile.ViewId)
            .HasConstraintName("fk_view_tiles_view_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
