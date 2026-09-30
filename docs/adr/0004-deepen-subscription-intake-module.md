# ADR 0004: Deepen Subscription Intake Module & Idempotent Unsubscribe

## Status
Accepted

## Context
Previously, subscription creation and cancellation were orchestrated across the HTTP transport layer:
1. `SubscriptionEndpoints` directly injected both `IArtistVerificationService` and `ISubscriptionService`, managing the verification workflow and domain validation before calling persistence.
2. `SubscriptionService` returned multi-element tuples `(Guid SubscriptionId, bool IsNew)` and `(bool Success, string Message)`.
3. In `SubscriptionEndpoints.Unsubscribe`, the endpoint inspected returned prose with magic string sniffing (`message.Contains("not currently subscribed")`) to conditionally branch between `SubscriptionHtmlRenderer.NotSubscribed` and `SubscriptionHtmlRenderer.Success`.
4. From the user's mental model, clicking "Unsubscribe" means "Ensure I do not receive emails." Differentiating between active and already-deactivated subscriptions in the UI introduced needless UI branching and brittle string inspection.

## Decision
1. **Deepen `ISubscriptionService` / `SubscriptionService`:**
   - Inject `IArtistVerificationService` directly into `SubscriptionService`.
   - Absorb domain validation (`request.TryValidate`), artist existence verification, user retrieval, and subscription state changes into `SubscriptionService.SubscribeAsync`.
   - Return strongly typed domain outcome records (`SubscribeResult` and `UnsubscribeResult`) with explicit statuses (`SubscribeStatus` and `UnsubscribeStatus`), eliminating tuple returns.
2. **Purely Idempotent Unsubscribe:**
   - Treat unsubscribe as idempotent: if a valid unguessable token is provided, deactivate matching subscriptions (or do nothing if already inactive) and return `UnsubscribeStatus.Success`.
   - Delete `SubscriptionHtmlRenderer.NotSubscribed()` and eliminate all string parsing from `SubscriptionEndpoints`.
3. **Simplify `SubscriptionEndpoints`:**
   - `SubscriptionEndpoints` acts as a pure transport adapter, mapping `SubscribeRequest` to `subscriptionService.SubscribeAsync` and translating domain result records directly to `TypedResults` and `SubscriptionHtmlRenderer`.

## Consequences
- **Locality:** Validation, artist verification, and subscription state lifecycle reside in one cohesive module.
- **Seam Leak Eliminated:** Zero string sniffing or domain branching at the HTTP transport seam.
- **Single Test Surface:** The subscription lifecycle is thoroughly testable through `ISubscriptionService` with unit and integration tests.
- **Idempotency:** Unsubscribing is deterministic and idempotent for end users.
