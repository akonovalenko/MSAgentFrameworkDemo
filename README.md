# BitcoinAgent

A lightweight **demonstration AI assistant** built with **ASP.NET Core**, **Microsoft.Extensions.AI**, and **OpenAI-compatible models**.

The primary goal of this repository is to demonstrate a **custom middleware pipeline inspired by Microsoft Agent Framework concepts** in a .NET application, including validation, retry handling, rate limiting, auditing, correlation tracking, and tool execution orchestration.

> [!IMPORTANT]  
> **This repository is not a production cryptocurrency application.**
> 
> It is a **demonstration project** created for educational and architectural purposes to show how middleware can be composed around an AI agent built with **Microsoft.Extensions.AI** and related Microsoft Agent Framework concepts.
> 
> The project is intended for:
> 
> - learning and experimentation,
> 
> - middleware pipeline design,
> 
> - AI tool-calling integration,
> 
> - validation and retry patterns,
> 
> - structured logging and auditing,
> 
> - Clean Architecture examples.
> 
> It should **not** be used for real trading, investment decisions, custody of funds, financial advice, or production cryptocurrency operations.

---

## Features

- 🤖 AI chat endpoint (`/chat`)

- 💰 Real-time Bitcoin price retrieval via CoinGecko

- 🧠 OpenAI-compatible LLM integration

- 🧰 Tool calling support (`GetCurrentBitcoinPrice`)

- 🔁 Automatic retry for transient failures

- ✅ LLM response validation

- ✅ Tool result validation

- 🚦 Per-user rate limiting

- 🛡️ Prompt validation and input protection

- 🪵 Structured logging and auditing

- 🔗 Correlation IDs for request tracing

- 🩺 Health checks (`/health`)

- 📦 Docker support

---

# Architecture

The solution follows a layered architecture inspired by Clean Architecture principles.

```text
BitcoinAgent.Api              # ASP.NET Core Minimal API
BitcoinAgent.Application      # Application layer, pipeline, middleware
BitcoinAgent.Domain           # Domain models and contracts
BitcoinAgent.Infrastructure   # OpenAI client, CoinGecko client, tools
```

## Layer responsibilities

| Layer              | Responsibility                                       |
| ------------------ | ---------------------------------------------------- |
| **Api**            | HTTP endpoints, Swagger, exception handling          |
| **Application**    | Agent orchestration, middleware pipeline, validators |
| **Domain**         | Models, interfaces, shared constants                 |
| **Infrastructure** | External services (OpenAI, CoinGecko)                |

---

# Request flow

```text
HTTP /chat
   ↓
BitcoinAgent
   ↓
AgentPipeline
   ↓
Middleware chain
   ↓
BitcoinAgentHandler
   ↓
LLM + Tools
   ↓
Response
```

---

# Middleware pipeline

The project demonstrates a **custom ordered middleware pipeline** similar in spirit to ASP.NET Core middleware.

| Middleware                        | Purpose                           |
| --------------------------------- | --------------------------------- |
| `CorrelationMiddleware`           | Assigns a correlation ID          |
| `RateLimitMiddleware`             | Limits requests per user          |
| `PromptValidationMiddleware`      | Validates prompt size and content |
| `LoggingMiddleware`               | Execution timing logs             |
| `AuditMiddleware`                 | Request/response audit logs       |
| `ExceptionMiddleware`             | Centralized exception logging     |
| `RetryMiddleware`                 | Retries transient failures        |
| `TokenUsageMiddleware`            | Logs LLM token usage              |
| `LLMResponseValidationMiddleware` | Validates final LLM response      |
| `ToolValidationMiddleware`        | Validates tool output             |

`PromptValidationMiddleware` executes early in the pipeline and rejects invalid requests before they reach logging, auditing, external tools, or the LLM. By default, prompts longer than **4000 characters** are rejected with a validation error.

---

# Technology stack

- **.NET 10**

- **ASP.NET Core Minimal API**

- **Microsoft.Extensions.AI**

- **OpenAI SDK**

- **Swagger / OpenAPI**

- **Docker**

---

# Prerequisites

- .NET 10 SDK

- OpenAI-compatible API key

- Internet access for CoinGecko API

---

# Configuration

Edit `BitcoinAgent.Api/appsettings.json`:

```json
{
  "OpenAI": {
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": "YOUR_API_KEY",
    "Model": "gpt-5",
    "Temperature": 0.2,
    "MaxOutputTokens": 2048
  },
  "CoinGecko": {
    "BaseUrl": "https://api.coingecko.com/",
    "TimeoutSeconds": 10
  },
  "PromptValidation": {
    "MaxPromptLength": 4000
  }
}
```

## Recommended for local development

Use **User Secrets** instead of storing the API key in the repository:

```bash
dotnet user-secrets init --project BitcoinAgent.Api
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_API_KEY" --project BitcoinAgent.Api
```

---

# Running locally

```bash
dotnet restore
dotnet build
dotnet run --project BitcoinAgent.Api
```

The API will be available at:

- Swagger UI: `https://localhost:60026/swagger`

- Health check: `https://localhost:60026/health`

---

# API

## Chat

**POST** `/chat`

### Request

```json
{
  "message": "What is the current Bitcoin price?"
}
```

### Response

```json
{
  "response": "The current Bitcoin price is approximately $118,000 USD."
}
```

### Validation error

```json
{
  "title": "Validation Error",
  "detail": "Message cannot be empty."
}
```

### Prompt too long

```json
{
  "title": "Validation Error",
  "detail": "Prompt is too long. Maximum allowed length is 4000 characters."
}
```

---

# Tool calling

The LLM decides whether to call the Bitcoin tool.

Supported tool:

```text
GetCurrentBitcoinPrice
```

The application executes the tool, validates the result, and sends the result back to the model to generate a natural-language response.

---

# Retry behavior

The agent automatically retries:

- `HttpRequestException`

- `TimeoutException`

- Empty or invalid LLM responses requested by validation middleware

Maximum attempts: **3**.

---

# Rate limiting

Default configuration:

- **10 requests per minute per user**

Anonymous users share a common bucket.

---

# Logging and auditing

Each request receives a correlation ID:

```text
CorrelationId=8f3a1c4b5d7e4f3a9b8c1d2e3f4a5b6c
```

Audit logs include:

- Prompt

- Duration

- Success/failure status

- Error message (without duplicate stack traces)

---

# Docker

## Build

```bash
docker build -t bitcoin-agent .
```

## Run

```bash
docker run -p 8080:8080 \
  -e OpenAI__ApiKey=YOUR_API_KEY \
  bitcoin-agent
```

API:

```text
http://localhost:8080
```

---

# Docker Compose

```bash
docker compose up --build
```

---

# Project structure

```text
BitcoinAgent.Api/
  Program.cs
  DependencyInjection.cs

BitcoinAgent.Application/
  AgentPipeline.cs
  BitcoinAgentHandler.cs
  Middleware/
  Validators/
  Memory/

BitcoinAgent.Domain/
  Models/
  IBitcoinTool.cs

BitcoinAgent.Infrastructure/
  Services/
  Tools/
  Options/
```

---

# Development notes

- `PromptValidationMiddleware` centralizes prompt validation and keeps HTTP endpoints free from duplicated validation logic.

---

# Extending the agent

## Add a new tool

1. Create a domain contract.

2. Implement the tool in `Infrastructure/Tools`.

3. Register it in `Infrastructure/DependencyInjection.cs`.

4. Add validation if needed.

5. Update the handler prompt/tool registration.

## Add a new middleware

Implement `IOrderedMiddleware` and register it in `Application/DependencyInjection.cs`.

---

# Health check

```bash
curl http://localhost:8080/health
```

Expected response:

```text
Healthy
```

---

# Example requests

## General question

```bash
curl -X POST http://localhost:8080/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello"}'
```

## Bitcoin price

```bash
curl -X POST http://localhost:8080/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"What is the Bitcoin price now?"}'
```

---

# Testing ideas

- Unit test validators

- Unit test middleware ordering

- Integration test `/chat`

- Mock `IBitcoinTool`

- Mock `IChatClient`

---

# Roadmap

- Persistent conversation memory

- Streaming responses

- Prometheus metrics

- Grafana dashboards

- Distributed rate limiting (Redis)

- Multiple tools

- Authentication and authorization

- OpenTelemetry tracing

---

# Educational purpose

This repository is intentionally kept relatively small and easy to read. The focus is on demonstrating:

- middleware composition,

- pipeline ordering,

- cross-cutting concerns,

- AI tool orchestration,

- validation patterns,

- retry semantics,

- structured logging,

- request auditing.

It is designed to be used as a **reference implementation, learning resource, interview discussion project, or architectural prototype**, not as a production AI platform.

---

# License

MIT License.

---

# Acknowledgements

- [OpenAI](https://platform.openai.com/)

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)

- [CoinGecko API](https://www.coingecko.com/en/api)
