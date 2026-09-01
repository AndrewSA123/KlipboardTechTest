export type Severity = 'Routine' | 'NeedsAttention' | 'SafetyCritical';

export interface FaultAnalysis {
  summary: string;
  affectedSystems: string[];
  severity: Severity;
  clarifyingQuestions: string[];
  suggestedNextSteps: string[];
}