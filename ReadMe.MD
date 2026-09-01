# Klipboard Fault Triage Assistant

A small application that helps a garage's service desk turn free-text customer fault
descriptions into a structured triage summary, using an LLM (via Groq) to identify
affected systems, severity, clarifying questions, and suggested next steps.

## Tech stack

- **Backend**: .NET 10, ASP.NET Core Web API (controllers)
- **Frontend**: React + TypeScript (Vite), Tailwind CSS
- **AI**: Groq API (OpenAI-compatible), `openai/gpt-oss-120b`

## Project structure

```
backend/
  FaultTriage.Api/             # ASP.NET Core Web API
  FaultTriage.Core/            # Domain models, IFaultAnalyser interface
  FaultTriage.Infrastructure/  # Groq client implementation
  FaultTriage.Tests/           # Unit tests
frontend/                      # React + TypeScript + Tailwind app
docs/                          # Supporting documentation (AI usage notes, etc.)
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) (v18+) and npm
- A free [Groq API key](https://console.groq.com) — see setup below

## Configuration

### 1. Get a Groq API key

1. Sign up at [console.groq.com](https://console.groq.com) (free, no card required).
2. Go to **API Keys** and click **Create API Key**.
3. Copy the key immediately — Groq only shows it once.

### 2. Add it to .NET user-secrets (backend)

We use [.NET user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
to keep the API key out of source control entirely — it's stored outside the repo, in a
per-user file on your machine, so it's never at risk of being committed.

From the `backend/FaultTriage.Api` directory:

```bash
cd backend/FaultTriage.Api
dotnet user-secrets init
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key-here"
```

That's it — no `.env` file, no config file to edit for the backend. The app reads the key
from user-secrets automatically when running in the `Development` environment (the default
for `dotnet run`).

To verify it's set (without printing the key itself to a shared screen or chat):

```bash
dotnet user-secrets list
```

You should see a line starting `Groq:ApiKey = ...`.

### 3. Set the API URL (frontend)

The frontend needs to know where the backend is running. Copy the example env file and
adjust the port if needed:

```bash
cd frontend
cp .env.example .env
```

`frontend/.env.example` contains:
```
VITE_API_BASE_URL=https://localhost:7123
```

Replace the port with whatever `dotnet run` prints for the API (see below) if it differs.
`.env` is git-ignored since it's environment-specific; `.env.example` is committed so
anyone cloning the repo knows what to set.

## Running the application

You'll need **two terminals** running at the same time.

### Terminal 1 — backend

```bash
cd backend/FaultTriage.Api
dotnet run
```

Note the HTTPS URL it prints (e.g. `https://localhost:7123`) — this is your API base URL,
and should match `VITE_API_BASE_URL` in `frontend/.env`.

### Terminal 2 — frontend

```bash
cd frontend
npm install   # first time only
npm run dev
```

Open the URL it prints (default `http://localhost:5173`) in your browser.

Paste a customer fault description into the textarea (or click one of the example
descriptions) and click **Analyze fault** to see the structured result.

## Running tests

```bash
cd backend/FaultTriage.Tests
dotnet test
```

Covers the Groq client's success path, HTTP error handling, malformed model responses,
and empty-input validation, using a faked `HttpMessageHandler` — no real network calls or
API key required to run the tests.

## API reference

`POST /api/faults/analyze`

Request:
```json
{ "description": "grinding noise when braking, engine light on" }
```

Response:
```json
{
  "summary": "...",
  "affectedSystems": ["Brakes", "Engine"],
  "severity": "SafetyCritical",
  "clarifyingQuestions": ["..."],
  "suggestedNextSteps": ["..."]
}
```

A Postman collection covering the happy path and error cases is available in
[`postman/`](./postman) if you'd rather test the API directly than through the UI.

## Notes on trade-offs

See [`docs/AI-USAGE.md`](./docs/AI-USAGE.md) for prompt design reasoning, model choice, and
decisions made under the exercise's time constraint.