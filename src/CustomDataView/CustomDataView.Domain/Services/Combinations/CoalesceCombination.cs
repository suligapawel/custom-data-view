namespace CustomDataView.Domain.Services.Combinations;

public sealed class CoalesceCombination : Combination
{
    public override string Key => "coalesce";

    public override string Name => "Pierwsza wartość";

    public override string Description => "Zwraca pierwszą wartość pól różną od braku wartości.";

    public override int MinFields => 2;
}
