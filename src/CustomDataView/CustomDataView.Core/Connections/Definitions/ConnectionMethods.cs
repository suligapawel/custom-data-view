namespace CustomDataView.Core.Connections.Definitions;

public static class ConnectionMethods
{
    public const string Get = "GET";
    public const string Post = "POST";

    public static bool IsGet(string method)
    {
        return string.Equals(method, Get, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPost(string method)
    {
        return string.Equals(method, Post, StringComparison.OrdinalIgnoreCase);
    }

    public static string Of(ConnectionDefinition definition)
    {
        return definition is PostConnectionDefinition ? Post : Get;
    }
}
