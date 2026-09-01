import type { Severity } from '../types/faultAnalysis';

export const SEVERITY_STYLES: Record<Severity, string> = {
  Routine: 'bg-green-100 text-green-800',
  NeedsAttention: 'bg-amber-100 text-amber-800',
  SafetyCritical: 'bg-red-100 text-red-800',
};