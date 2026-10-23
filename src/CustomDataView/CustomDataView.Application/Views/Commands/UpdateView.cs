using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Views.Validation;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Views.Commands;

public sealed record UpdateView(Guid Id, ViewDefinition Definition) : ICommand;

public sealed class UpdateViewHandler(IViewRepository repository, ViewReferencesValidator referencesValidator)
{
    public void Handle(UpdateView command)
    {
        var view = repository.Get(command.Id) ?? throw new NotFoundException(command.Id);
        referencesValidator.EnsureValid(command.Definition);
        repository.Update(view with { Definition = command.Definition });
    }
}
