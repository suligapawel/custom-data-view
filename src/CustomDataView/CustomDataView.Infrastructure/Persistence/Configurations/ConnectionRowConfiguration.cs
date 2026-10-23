using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class ConnectionRowConfiguration : IEntityTypeConfiguration<ConnectionRow>
{
    public void Configure(EntityTypeBuilder<ConnectionRow> builder)
    {
        builder.ToTable("connections", table =>
        {
            table.HasCheckConstraint("ck_connections_method", "method IN ('GET', 'POST')");
            table.HasCheckConstraint("ck_connections_body", "method = 'POST' OR body IS NULL");
        });
        builder.HasKey(connection => connection.Id).HasName("pk_connections");
        builder.Property(connection => connection.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(connection => connection.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(connection => connection.Method).HasColumnName("method").HasMaxLength(4).IsRequired();
        builder.Property(connection => connection.Url).HasColumnName("url").HasColumnType("text").IsRequired();
        builder.Property(connection => connection.Body).HasColumnName("body").HasColumnType("json");
        builder.Property(connection => connection.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(connection => connection.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.HasIndex(connection => new { connection.CreatedAt, connection.Id })
            .HasDatabaseName("ix_connections_created_at_id");
        builder.HasMany(connection => connection.PathParameters)
            .WithOne()
            .HasForeignKey(parameter => parameter.ConnectionId)
            .HasConstraintName("fk_connection_path_parameters_connection_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(connection => connection.QueryParameters)
            .WithOne()
            .HasForeignKey(parameter => parameter.ConnectionId)
            .HasConstraintName("fk_connection_query_parameters_connection_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(connection => connection.BodyParameters)
            .WithOne()
            .HasForeignKey(parameter => parameter.ConnectionId)
            .HasConstraintName("fk_connection_body_parameters_connection_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
