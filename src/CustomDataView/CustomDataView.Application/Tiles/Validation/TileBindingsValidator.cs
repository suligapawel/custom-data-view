using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Application.Tiles.Validation;

public static class TileBindingsValidator
{
    public static void Validate(TileInput input, Dictionary<string, string[]> errors)
    {
        var sources = input.Sources
            .Where(source => TileNodeKeys.IsValid(source.Key))
            .GroupBy(source => source.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var pairs = new HashSet<(string, string)>();
        var targets = new HashSet<(string, string, string)>();
        for (var index = 0; index < input.Bindings.Count; index++)
        {
            var binding = input.Bindings[index];
            var key = $"bindings[{index}]";
            var nodesAreValid = true;
            if (!sources.ContainsKey(binding.From ?? string.Empty))
            {
                errors[$"{key}.from"] = [$"Węzeł '{binding.From}' nie istnieje w kafelku."];
                nodesAreValid = false;
            }

            if (!sources.ContainsKey(binding.To ?? string.Empty))
            {
                errors[$"{key}.to"] = [$"Węzeł '{binding.To}' nie istnieje w kafelku."];
                nodesAreValid = false;
            }
            else if (binding.To == binding.From)
            {
                errors[$"{key}.to"] = ["Węzeł docelowy musi być różny od źródłowego."];
                nodesAreValid = false;
            }

            if (nodesAreValid && !pairs.Add((binding.From, binding.To)))
            {
                errors[key] = [$"Wiązanie '{binding.From}' → '{binding.To}' występuje w kafelku więcej niż raz."];
            }

            if (nodesAreValid && (sources[binding.From].Kind != TileSourceKinds.Connection || sources[binding.To].Kind != TileSourceKinds.Connection))
            {
                errors[$"{key}.to"] = ["Wiązanie źródło → źródło łączy tylko źródła rodzaju 'connection'."];
                nodesAreValid = false;
            }

            if (binding.Map.Count == 0)
            {
                errors[$"{key}.map"] = ["Wiązanie musi mieć co najmniej jedno mapowanie."];
            }

            for (var position = 0; position < binding.Map.Count; position++)
            {
                ValidateMapping(input, binding, binding.Map[position], $"{key}.map[{position}]", nodesAreValid, targets, errors);
            }
        }

        var order = BindingGraph.Order(
            sources.Keys.ToList(),
            input.Bindings.Select(binding => (binding.From ?? string.Empty, binding.To ?? string.Empty)));
        if (order.Cycle.Count > 0)
        {
            errors.TryAdd("bindings", [$"Wiązania tworzą cykl między węzłami: {string.Join(", ", order.Cycle)}."]);
        }
    }

    private static void ValidateMapping(
        TileInput input,
        TileBindingInput binding,
        TileBindingMappingInput mapping,
        string key,
        bool nodesAreValid,
        HashSet<(string, string, string)> targets,
        Dictionary<string, string[]> errors)
    {
        var field = input.Fields.FirstOrDefault(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field));
        if (string.IsNullOrWhiteSpace(mapping.Field) || (nodesAreValid && field?.Source != binding.From))
        {
            errors[$"{key}.field"] = [$"Pole '{mapping.Field}' nie istnieje w węźle '{binding.From}'."];
        }
        else if (input.Items is not null && field?.Source == input.Items.Source)
        {
            errors[$"{key}.field"] = ["Pole elementu nie może zasilać wiązania źródło → źródło."];
        }

        var hasParameter = !string.IsNullOrWhiteSpace(mapping.Parameter);
        var hasInput = !string.IsNullOrWhiteSpace(mapping.Input);
        if (hasParameter == hasInput)
        {
            errors[key] = ["Mapowanie wskazuje dokładnie jedno z: parametr albo wejście danych."];
        }
        else if (hasInput)
        {
            errors[$"{key}.input"] = ["Źródło typu 'connection' nie ma wejść danych."];
        }
        else if (!targets.Add((binding.To, "parameter", mapping.Parameter)))
        {
            errors[$"{key}.parameter"] = [$"Parametr '{mapping.Parameter}' węzła '{binding.To}' ma już źródło w innym mapowaniu."];
        }

        if (mapping.Scope is not null)
        {
            errors[$"{key}.scope"] = ["Wiązanie źródło → źródło nie przyjmuje zakresu."];
        }
    }
}
