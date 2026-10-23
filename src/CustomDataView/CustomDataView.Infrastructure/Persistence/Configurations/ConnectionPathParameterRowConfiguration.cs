using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class ConnectionPathParameterRowConfiguration : IEntityTypeConfiguration<ConnectionPathParameterRow>
{
    public void Configure(EntityTypeBuilder<ConnectionPathParameterRow> builder)
    {
        builder.ToTable("connection_path_parameters");
        builder.HasKey(parameter => new { parameter.ConnectionId, parameter.Name }).HasName("pk_connection_path_parameters");
        builder.Property(parameter => parameter.ConnectionId).HasColumnName("connection_id");
        builder.Property(parameter => parameter.Name).HasColumnName("name").HasMaxLength(50);
        builder.Property(parameter => parameter.Required).HasColumnName("required");
        builder.Property(parameter => parameter.DefaultValue).HasColumnName("default_value").HasColumnType("text");
    }
}
