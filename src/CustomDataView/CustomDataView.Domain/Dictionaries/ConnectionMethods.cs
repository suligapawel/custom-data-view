namespace CustomDataView.Domain.Dictionaries;

public static class ConnectionMethods
{
    public const string Get = "GET";
    public const string Post = "POST";

    public static bool IsGet(string method) => string.Equals(method, Get, StringComparison.OrdinalIgnoreCase);
    public static bool IsPost(string method) => string.Equals(method, Post, StringComparison.OrdinalIgnoreCase);
}
