namespace FaultTriage.Core;

public interface IFaultAnalyser
{
    Task<FaultAnalysis> AnalyzeAsync(string faultDescription, CancellationToken cancellationToken = default);
}