## Configuration

This project uses the [Groq API](https://console.groq.com) for AI-powered fault analysis. You'll need your own free Groq API key to run it.

### 1. Get a Groq API key

1. Sign up at [console.groq.com](https://console.groq.com) (free, no card required).
2. Go to **API Keys** and click **Create API Key**.
3. Copy the key immediately — Groq only shows it once.

### 2. Add it to .NET user-secrets

We use [.NET user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep the API key out of source control entirely — it's stored outside the repo, in a per-user file on your machine, so it's never at risk of being committed.

From the `backend/FaultTriage.Api` directory:

```bash
cd backend/FaultTriage.Api
dotnet user-secrets init
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key-here"
```

That's it — no `.env` file, no config file to edit. The app reads the key from user-secrets automatically when running in the `Development` environment (the default for `dotnet run`).

To verify it's set:

```bash
dotnet user-secrets list
```

You should see `Groq:ApiKey = your-groq-api-key-here` in the output.