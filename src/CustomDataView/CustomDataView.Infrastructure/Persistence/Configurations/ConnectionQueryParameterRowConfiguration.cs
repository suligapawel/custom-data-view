using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomDataView.Infrastructure.Persistence.Configurations;

public sealed class ConnectionQueryParameterRowConfiguration : IEntityTypeConfiguration<ConnectionQueryParameterRow>
{
    public void Configure(EntityTypeBuilder<ConnectionQueryParameterRow> builder)
    {
        builder.ToTable("connection_query_parameters");
        builder.HasKey(parameter => new { parameter.ConnectionId, parameter.Name }).HasName("pk_connection_query_parameters");
        builder.Property(parameter => parameter.ConnectionId).HasColumnName("connection_id");
        builder.Property(parameter => parameter.Name).HasColumnName("name").HasColumnType("text");
        builder.Property(parameter => parameter.Required).HasColumnName("required");
        builder.Property(parameter => parameter.DefaultValue).HasColumnName("default_value").HasColumnType("text");
    }
}
