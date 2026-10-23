namespace CustomDataView.Application.Exceptions;

public sealed class NotFoundException(Guid id) : Exception($"Resource {id} was not found.")
{
    public Guid Id { get; } = id;
}
