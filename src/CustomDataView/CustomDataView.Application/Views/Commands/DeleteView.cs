using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.Application.Views.Commands;

public sealed record DeleteView(Guid Id) : ICommand;

public sealed class DeleteViewHandler(IViewRepository repository)
{
    public void Handle(DeleteView command)
    {
        _ = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        repository.Delete(command.Id);
    }
}
