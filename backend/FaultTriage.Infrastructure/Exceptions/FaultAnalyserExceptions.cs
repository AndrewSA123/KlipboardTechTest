namespace FaultTriage.Infrastructure.Exceptions;

public class FaultAnalyserException : Exception
{
    public FaultAnalyserException(string message) : base(message) { }
    public FaultAnalyserException(string message, Exception innerException) : base(message, innerException) { }
}