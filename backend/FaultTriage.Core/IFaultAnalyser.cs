namespace FaultTriage.Core;

public interface IFaultAnalyzer
{
    Task<FaultAnalysis> AnalyzeAsync(string faultDescription, CancellationToken cancellationToken = default);
}