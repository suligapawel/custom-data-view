using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Views.Validation;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Views.Commands;

public sealed record AddView(ViewDefinition Definition) : ICommand<Guid>;

public sealed class AddViewHandler(IViewRepository repository, ViewReferencesValidator referencesValidator)
{
    public Guid Handle(AddView command)
    {
        referencesValidator.EnsureValid(command.Definition);
        var view = new View(Guid.NewGuid(), command.Definition);
        repository.Add(view);
        return view.Id;
    }
}
