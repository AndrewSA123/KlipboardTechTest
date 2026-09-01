namespace FaultTriage.Core;

public record FaultAnalysis(
    string Summary,
    IReadOnlyList<string> AffectedSystems,
    Severity Severity,
    IReadOnlyList<string> ClarifyingQuestions,
    IReadOnlyList<string> SuggestedNextSteps
);