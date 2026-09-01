import type { FaultAnalysis } from '../types/faultAnalysis';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export class FaultAnalysisError extends Error {
  status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = 'FaultAnalysisError';
    this.status = status;
  }
}

export async function analyzeFault(description: string): Promise<FaultAnalysis> {
  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}/api/faults/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ description }),
    });
  } catch {
    throw new FaultAnalysisError('Could not reach the server. Is the API running?');
  }

  if (!response.ok) {
    const errorText = await response.text().catch(() => '');
    throw new FaultAnalysisError(
      errorText || `Request failed with status ${response.status}`,
      response.status
    );
  }

  return response.json() as Promise<FaultAnalysis>;
}