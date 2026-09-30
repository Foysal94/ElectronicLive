# ADR 0003: HTML Email Digest Template & Dispatcher Architecture

## Status
Accepted

## Context
When scheduled background scans discover new live shows for artists on a user's watchlist, the system must generate and dispatch a transactional email digest.

Key technical requirements and constraints:
1. **Local Development Experience:** Developers must be able to run and verify the full notification loop locally with zero external API credentials, zero internet access, and no risk of accidental outbound emails.
2. **Production Delivery:** Production relies on Resend REST API (`POST https://api.resend.com/emails`) using API key authentication provisioned via Azure Container Apps secrets.
3. **Template Maintainability & Deployment Safety:** Loose HTML template files on disk frequently cause runtime `FileNotFoundException` crashes in Docker/Linux containerized environments due to path resolution discrepancies. Conversely, embedding extensive HTML `<table>` markup directly inside C# strings clutters business logic and loses HTML/CSS syntax tooling.
4. **Multi-City Scalability:** The template and subject line must support future geographical expansion beyond London without hardcoding cities in static headers.

## Decision
1. **Dispatcher Abstraction (`IEmailDispatcher`):**
   - Declare `Task SendDigestAsync(string toEmail, string artistName, IReadOnlyList<EventResponse> newEvents, string unsubscribeUrl, CancellationToken ct)`.
2. **Implementations:**
   - `LoggingEmailDispatcher`: Logs formatted email summaries to the logger and writes inspectable `.html` preview files to `.scratch/emails/` during local development and testing.
   - `ResendEmailDispatcher`: Production dispatcher calling Resend REST API via typed `HttpClient` with Bearer token authentication.
3. **Configuration & DI Registration (`AddEmailDispatching`):**
   - Register `ResendEmailDispatcher` when `Resend:ApiKey` is present in configuration; automatically fallback to `LoggingEmailDispatcher` when the key is missing or blank.
   - Default "From" sender address configured as `ElectronicLive <alerts@electroniclive.co.uk>`, overrideable via `Resend:FromEmail` in `appsettings.secrets.json` for sandbox testing (`onboarding@resend.dev`).
4. **Embedded Resource Template (`EmailTemplateBuilder`):**
   - Place the dark-mode responsive HTML/CSS template in `Background/Email/Templates/DigestEmailTemplate.html` marked as an `EmbeddedResource` in `ElectronicLive.Api.csproj`.
   - `EmailTemplateBuilder` loads the embedded template stream from the assembly and performs safe HTML encoding and token substitution for brand header, event cards, multi-provider ticket buttons, and unsubscribe links.
5. **Digest Content & Copy:**
   - Subject line: `New shows announced: {artistName}`.
   - Branded header: `ElectronicLive | EDM & Live Gig Tracker`.
   - Multi-provider buttons: Renders dedicated links for each ticketing provider (Ticketmaster, Resident Advisor, Skiddle) with active and sold-out states.

## Consequences
- **Deployment Reliability:** Embedded resource compilation guarantees the email template is physically present inside the DLL binary, eliminating Docker runtime path resolution failures.
- **Developer Workflow:** Local runs and unit tests produce rendered `.html` preview files for rapid visual verification without external dependencies or Resend costs.
- **Maintainability:** HTML table styling and CSS remain in a dedicated `.html` file with editor syntax highlighting, while C# rendering logic remains lightweight, testable, and strongly typed.
