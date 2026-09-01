import { useState } from 'react';
import { analyzeFault, FaultAnalysisError } from './api/faultsApi';
import type { FaultAnalysis } from './types/faultAnalysis';
import { SEVERITY_STYLES } from './constants/severityStyles';

type RequestState = 'idle' | 'loading' | 'error';

const EXAMPLE_DESCRIPTIONS = [
  'Grinding noise when I brake, also the engine light came on last week',
  "Car won't start in the mornings, battery is only a year old",
  'Steering wheel shakes at motorway speeds',
];

function App() {
  const [description, setDescription] = useState('');
  const [state, setState] = useState<RequestState>('idle');
  const [result, setResult] = useState<FaultAnalysis | null>(null);
  const [errorMessage, setErrorMessage] = useState('');

  const handleAnalyze = async () => {
    if (!description.trim()) return;

    setState('loading');
    setErrorMessage('');

    try {
      const analysis = await analyzeFault(description);
      setResult(analysis);
      setState('idle');
    } catch (err) {
      const message =
        err instanceof FaultAnalysisError
          ? err.message
          : 'Something unexpected went wrong. Please try again.';
      setErrorMessage(message);
      setState('error');
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 py-10 px-4">
      <div className="mx-auto max-w-2xl">
        <h1 className="text-2xl font-semibold text-slate-900">
          Fault Triage Assistant
        </h1>
        <p className="mt-1 text-sm text-slate-500">
          Paste a customer's fault description to get a structured summary for the workshop.
        </p>

        <div className="mt-6">
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="e.g. grinding noise when braking, also engine light came on"
            rows={4}
            className="w-full rounded-lg border border-slate-300 p-3 text-sm text-slate-900 shadow-sm focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500"
          />

          <div className="mt-2 flex flex-wrap gap-2">
            {EXAMPLE_DESCRIPTIONS.map((example) => (
              <button
                key={example}
                type="button"
                onClick={() => setDescription(example)}
                className="rounded-full border border-slate-200 bg-white px-3 py-1 text-xs text-slate-600 hover:bg-slate-100"
              >
                {example}
              </button>
            ))}
          </div>

          <button
            type="button"
            onClick={handleAnalyze}
            disabled={state === 'loading' || !description.trim()}
            className="mt-4 rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {state === 'loading' ? 'Analyzing…' : 'Analyze fault'}
          </button>
        </div>

        {state === 'error' && (
          <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {errorMessage}
          </div>
        )}

        {result && state !== 'error' && (
          <div className="mt-6 rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-medium text-slate-500">Summary</h2>
              <span
                className={`rounded-full px-3 py-1 text-xs font-medium ${SEVERITY_STYLES[result.severity]}`}
              >
                {result.severity}
              </span>
            </div>
            <p className="mt-1 text-slate-900">{result.summary}</p>

            <h2 className="mt-4 text-sm font-medium text-slate-500">Affected systems</h2>
            <div className="mt-1 flex flex-wrap gap-2">
              {result.affectedSystems.map((system) => (
                <span
                  key={system}
                  className="rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-700"
                >
                  {system}
                </span>
              ))}
            </div>

            {result.clarifyingQuestions.length > 0 && (
              <>
                <h2 className="mt-4 text-sm font-medium text-slate-500">
                  Clarifying questions
                </h2>
                <ul className="mt-1 list-inside list-disc text-sm text-slate-700">
                  {result.clarifyingQuestions.map((q) => (
                    <li key={q}>{q}</li>
                  ))}
                </ul>
              </>
            )}

            <h2 className="mt-4 text-sm font-medium text-slate-500">Suggested next steps</h2>
            <ul className="mt-1 list-inside list-disc text-sm text-slate-700">
              {result.suggestedNextSteps.map((step) => (
                <li key={step}>{step}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;