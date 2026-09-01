using System;
using System.Collections.Generic;
using System.Text;

namespace FaultTriage.Infrastructure.Models.Groq;

internal record GroqChatCompletionResponse(List<GroqChoice> Choices);