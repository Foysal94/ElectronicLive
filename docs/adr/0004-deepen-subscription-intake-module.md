# ADR 0004: Deepen Subscription Intake Module & Idempotent Unsubscribe

## Status
Accepted

## Context
Previously, subscription creation and cancellation were orchestrated across the HTTP transport layer:
1. `SubscriptionEndpoints` directly injected both `IArtistVerificationService` and `ISubscriptionService`, managing the verification workflow and domain validation before calling persistence.
2. `SubscriptionService` returned multi-element tuples `(Guid SubscriptionId, bool IsNew)` and `(bool Success, string Message)`.
3. In `SubscriptionEndpoints.Unsubscribe`, the endpoint inspected returned prose with magic string sniffing (`message.Contains("not currently subscribed")`) to conditionally branch between `SubscriptionHtmlRenderer.NotSubscribed` and `SubscriptionHtmlRenderer.Success`.
4. From the user's mental model, clicking "Unsubscribe" means "Ensure I do not receive emails." Differentiating between active and already-deactivated subscriptions in the UI introduced needless UI branching and brittle string inspection.
5. In `Services/`, files were unstructured across multiple domains without feature separation.

## Decision
1. **Feature Organization (`Services/Subscriptions/`):**
   - Co-locate all subscription domain logic in `Services/Subscriptions/` (`ISubscriptionService`, `SubscriptionService`, `SubscribeOutcome`, `UnsubscribeOutcome`).
2. **Deepen `ISubscriptionService` / `SubscriptionService`:**
   - Inject `IArtistVerificationService` directly into `SubscriptionService`.
   - `SubscriptionService.SubscribeAsync` encapsulates artist existence verification, user retrieval, and subscription state changes, returning a lightweight `SubscribeOutcome(Guid SubscriptionId, bool IsNew)` or `null` if verification fails.
   - `SubscriptionService.UnsubscribeAsync` returns an `UnsubscribeOutcome` enum (`Success`, `MissingToken`, `InvalidToken`).
3. **Purely Idempotent Unsubscribe:**
   - Treat unsubscribe as idempotent: if a valid unguessable token is provided, deactivate matching subscriptions (or do nothing if already inactive) and return `UnsubscribeOutcome.Success`.
   - Delete `SubscriptionHtmlRenderer.NotSubscribed()` and eliminate all string parsing from `SubscriptionEndpoints`.
4. **Thin Transport Adapter (`SubscriptionEndpoints`):**
   - `SubscriptionEndpoints` acts as a pure transport adapter, validating request syntax immediately with `request.TryValidate()`, calling `subscriptionService.SubscribeAsync`, and translating outcomes directly to `TypedResults` and `SubscriptionHtmlRenderer`.

## Consequences
- **Locality:** Validation, artist verification, and subscription state lifecycle reside in a dedicated feature module (`Services/Subscriptions/`).
- **Zero String Sniffing:** Brittle string matching deleted from endpoint handlers.
- **Minimal Surface:** Uses only two lean domain types (`SubscribeOutcome`, `UnsubscribeOutcome`) with zero ceremonial wrapper layers.
- **Single Test Surface:** Thoroughly testable with unit and integration tests asserting observable state.
- **Idempotency:** Unsubscribing is deterministic and idempotent for end users.
