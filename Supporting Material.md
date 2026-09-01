# Disclaimer

AI was used in every part of this project, even the documentation below.
While I relied on AI to create files, edit code, and asist in working this document,
all decisions and choices are my own and I stand by everything in this project.

# AI Usage & Design Notes

This document explains the design decisions made while building the Fault Triage
Assistant, particularly around how AI is used, why certain trade-offs were made under
the exercise's time constraint, and what I'd do differently with more time.

## The problem being solved

A garage receives free-text fault descriptions from customers ("grinding noise when
braking, engine light came on last week"). A service adviser has to read this, work out
what's actually wrong, decide how urgent it is, and figure out what to ask the customer
next. This application uses an LLM to do a first pass of that triage — structured,
fast, and consistent — while leaving the actual judgement call with the adviser.

The AI is explicitly framed as an assistant, not a decision-maker: it never books work,
assigns urgency without a human seeing it, or acts autonomously. It produces a
structured suggestion that a human reads before anything happens.

## Why Groq, and why `openai/gpt-oss-120b`

Groq is an inference provider — they don't train their own models, they serve
open-weight models (Llama, Qwen, GPT-OSS, etc.) at very high speed on custom hardware
(LPUs), with a genuinely free, no-card-required developer tier. That combination made
it a good fit for a time-boxed exercise: fast to set up, fast to iterate against
(low-latency responses during development), and no cost or billing setup required.

`openai/gpt-oss-120b` was chosen over a smaller/faster model because this task
benefits more from reasoning quality than from raw speed — correctly judging whether a
symptom is safety-critical matters more than shaving off a few hundred milliseconds.
It was chosen over a larger frontier closed model (e.g. GPT-4-class) because the task
is well-scoped and schema-constrained (JSON mode, fixed enum, short output) — the
marginal capability of a larger model matters less here than it would for open-ended
reasoning, and staying within Groq's free tier kept the whole exercise at zero cost.

**Trade-off**: this is not the most capable model available for the task. A frontier
model would likely handle ambiguous or multi-fault descriptions with more nuance. Given
the scope and time budget of this exercise, that gap was an acceptable trade for speed,
cost, and simplicity of setup.

## Architecture: the `IFaultAnalyser` abstraction

The LLM call is hidden behind a single interface (`FaultTriage.Core.IFaultAnalyser`),
implemented by `GroqFaultAnalyser` in the `Infrastructure` layer. The `Core` project
has no dependency on HTTP, JSON serialisation details, or Groq at all — it only knows
about the domain shape (`FaultAnalysis`, `Severity`).

This was a deliberate choice, not just "clean architecture for its own sake":

- **Provider swap is a one-file change.** Moving to Anthropic, OpenAI, Azure OpenAI, or
  a local Ollama model would mean writing a new class that implements
  `IFaultAnalyser` and changing one line of DI registration — nothing else in the
  application would need to know or care.
- **Testability.** The API controller and any future consumers depend on an interface,
  not a concrete HTTP client, so they're trivially mockable.
- **It proved its value mid-project**, not just in theory — Previously a different Groq
  model was chosen but was found to be deprecated, this design choice allowed for fast
  iteration.

## Prompt design

### System / user message split

The Groq request sends two messages: a `system` message with fixed, unchanging
instructions (act as a triage assistant, return only this JSON schema, treat brakes/
steering/tyres as safety-critical), and a `user` message containing only the raw
customer-provided fault text.

This split matters for a few reasons:
- It maps cleanly onto "constant configuration" (the system prompt) vs. "per-request
  variable input" (the fault description) — which is exactly the distinction the
  role system is designed for.
- Modern chat models are fine-tuned to weight system-role instructions more heavily
  than user-role content, so following this structure gets better instruction-following
  than concatenating everything into one block.
- It provides some resistance to prompt injection via the free-text input — since the
  customer-provided text is a separate, clearly-delineated user message rather than
  being blended into the instructions themselves, the model is less likely to treat
  adversarial text in a fault description as a new instruction.

### Temperature: 0.2

Set deliberately low. This task wants consistency, not creativity — the same fault
description should get a similar severity judgement each time, and low temperature
reduces the risk of malformed JSON or inconsistent classification. `0.0` would be
even more deterministic; `0.2` was chosen to leave a small amount of room for natural
phrasing variation in the free-text fields (`summary`, `suggestedNextSteps`) without
affecting the reliability of the structured classification fields.

### JSON mode over free-text parsing

The request uses Groq's `response_format: { type: "json_object" }` (OpenAI-compatible)
rather than asking the model to produce free text and parsing it with regex or string
matching. This is far more reliable, and any parsing failure is caught explicitly and
surfaced as a clear `FaultAnalyserException` rather than silently producing garbage
data.

### Inline prompt vs. a separate file

The system prompt is a `const string` inside `GroqFaultAnalyser.cs`, rather than being
extracted to a separate text file or embedded resource. For a project this size and
lifespan, that was the right call — it avoids file-path resolution and keeps the whole
class readable in one place, and the prompt isn't being iterated on independently of
the code by a non-developer. In a longer-lived or team project, I'd extract it to a
resource file, since prompt wording tends to get tuned frequently and separately from
application logic, and doing so would avoid needing a rebuild for a wording change.

## Error handling

Failures are treated as a first-class concern, not an afterthought:
- Empty/whitespace input is rejected before any HTTP call is made.
- Non-2xx responses from Groq (auth failures, model-not-found, rate limits) are caught
  and wrapped in a `FaultAnalyserException` with the upstream status and message
  preserved, rather than letting a raw `HttpRequestException` bubble up.
- Malformed or schema-mismatched JSON from the model is caught and wrapped the same
  way, rather than throwing an unhandled `JsonException`.
- The API controller maps `FaultAnalyserException` to a `502 Bad Gateway` with a
  generic, safe message to the client, while logging the real detail server-side.

Two of these failure paths (invalid API key, deprecated model → 404) were hit for real
during development, not just anticipated — both surfaced as clean, informative errors
rather than crashes, which was a good real-world validation of the approach.

## Security

- The Groq API key is never committed to source control. It's stored via
  `dotnet user-secrets` locally, which keeps it entirely outside the repository.
- `.gitignore` explicitly excludes `appsettings.Development.json`, `.env` files, and
  IDE/build artefacts (`bin/`, `obj/`, `.vs/`).
- During development, an API key was briefly pasted into a chat conversation while
  troubleshooting — it was treated as compromised immediately and rotated, rather than
  assuming a free-tier key posed no risk. Good key hygiene was treated as a habit worth
  practising even when the immediate stakes were low.

## Testing

Given the time budget, testing was scoped to the highest-value area: the Groq client,
since it's the piece with the most failure modes and external dependency. Four tests
cover:
1. A valid model response deserialises correctly into `FaultAnalysis`.
2. A non-2xx HTTP response throws `FaultAnalyserException` with the status preserved.
3. Malformed JSON from the model throws `FaultAnalyserException` rather than an
   unhandled parsing exception.
4. Empty/whitespace input throws before any HTTP call is attempted (verified using a
   fake `HttpMessageHandler` that throws if invoked at all).

**Explicitly out of scope**: frontend tests, and integration tests that hit a real
Groq endpoint. Both were reasonable to defer given the exercise's time box; the unit
tests above give confidence in the piece of the system most likely to break in
non-obvious ways.

## Frontend choices

- **React + TypeScript via Vite**: current standard tooling for this kind of project;
  fast dev server, minimal config overhead, and much faster to iterate on than
  Webpack-based tooling like Create React App.
- **Tailwind CSS**: chosen for speed of building a reasonably polished UI without
  hand-writing a separate stylesheet, appropriate for a project of this scope.
- **API base URL via a Vite environment variable** (`VITE_API_BASE_URL`), not
  hardcoded — a small amount of extra setup for a meaningfully more portable result.
- **Stateless by design**: nothing is persisted server-side. This matches the
  scenario's core need — quick triage of a single fault description — without adding
  a database for a three-hour exercise. If this were extended into a real product, the
  first addition would be persistence so an adviser's edits and decisions aren't lost
  on refresh.

## What I'd do with more time

- Make the result editable before an adviser "confirms" it, and persist the confirmed
  version — this was in the original plan but deprioritised in favour of a fully
  working, well-tested core flow within the time available.
- Add a small static lookup table mapping affected systems to labour/job codes, so the
  output is closer to something a workshop system could act on directly.
- Frontend tests for the UI's state transitions (loading, error, success).
- Extract the system prompt to a resource file if this were to become a longer-lived
  or multi-contributor project.
- Add basic rate-limiting/retry handling around the Groq call, since the free tier is
  rate-limited and a busy service desk would eventually hit that.

## Naming convention note

Identifiers, types, and file names throughout this codebase use British English
spelling (e.g. `IFaultAnalyser`, `GroqFaultAnalyser`, `FaultAnalyserException`) to match
the team's convention. Framework members and external API fields are left as-is where
British spelling isn't applicable — for example, `HttpClient.DefaultRequestHeaders.
Authorization` and Groq's own JSON field names are not renamed, since they belong to
.NET or to Groq's API contract rather than to this codebase's own naming choices.
