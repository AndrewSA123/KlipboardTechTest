namespace FaultTriage.Infrastructure;

public class FaultAnalyzerException : Exception
{
    public FaultAnalyzerException(string message) : base(message) { }
    public FaultAnalyzerException(string message, Exception innerException) : base(message, innerException) { }
}