namespace SummerProject.Server.Infrastructure.Database;

internal sealed class SqliteMigrationException : Exception
{
    public SqliteMigrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}