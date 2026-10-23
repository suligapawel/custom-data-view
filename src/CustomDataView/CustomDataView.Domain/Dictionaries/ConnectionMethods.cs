using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Dictionaries;

public static class ConnectionMethods
{
    private const string Get = "GET";
    private const string Post = "POST";

    public static bool IsGet(string method) => string.Equals(method, Get, StringComparison.OrdinalIgnoreCase);
    public static bool IsPost(string method) => string.Equals(method, Post, StringComparison.OrdinalIgnoreCase);
    public static string Of(ConnectionDefinition definition) => definition is PostConnectionDefinition ? Post : Get;
}
