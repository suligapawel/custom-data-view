using CustomDataView.Core.Connections.Calls;

namespace CustomDataView.Core.Tiles.Preview;

public sealed record ConnectionValuesInput(Guid? ConnectionId, ParameterValuesInput Parameters);
