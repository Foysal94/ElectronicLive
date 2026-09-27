# ADR 0001: Consolidate Event Search Module

## Status
Accepted

## Context
Previously, event searching was split across three shallow modules and interfaces:
1. `IEventAggregatorService` / `EventAggregatorService`: queried external provider adapters and called deduplication.
2. `IEventCacheService` / `EventCacheService`: passed through to `HybridCache.GetOrCreateAsync` with composite cache key formatting.
3. `IEventDeduplicator` / `EventDeduplicator`: performed venue string normalization and duplicate offer merging.

In `EventEndpoints`, the HTTP handler was forced to inject both `IEventCacheService` and `IEventAggregatorService`, orchestrating caching via a delegate closure on every request (`ct => aggregator.SearchEventsAsync(...)`). Furthermore, `IEventDeduplicator` was a single-implementation hypothetical seam mocked with NSubstitute in aggregator tests, preventing tests from asserting real deduplication and chronological ordering behavior across providers.

## Decision
1. Deepen the event search subsystem into a single `IEventSearchService` interface and `EventSearchService` implementation.
2. Absorb `HybridCache` caching policy and in-memory offer deduplication/sorting directly into `EventSearchService`.
3. Simplify `EventEndpoints` to inject only `IEventSearchService` and call `SearchEventsAsync`.
4. Delete the shallow interfaces and classes: `IEventAggregatorService`, `EventAggregatorService`, `IEventCacheService`, `EventCacheService`, `IEventDeduplicator`, and `EventDeduplicator`.
5. Retain `AllProvidersUnavailableException` as the domain error seam caught by `EventEndpoints` to return HTTP 502 Bad Gateway.

## Consequences
- **Locality**: Caching policy, provider fan-out, and offer merging reside in one cohesive module.
- **Test Surface**: Unit tests exercise the real `SearchEventsAsync` interface with in-memory provider stubs, validating end-to-end deduplication, sorting, and caching without mocking internal algorithmic helpers.
- **Interface Surface**: `EventEndpoints` is decoupled from caching orchestration and depends on a single narrow interface.
