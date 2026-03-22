# Epic: Postgres Baseline Seeding Worker

## Summary
Replace the legacy Cosmos/Azure Function population path with a scheduled worker that seeds the live Postgres model used by the app.

Initial scope is baseline data the web app depends on now:
- Competitions
- Drivers
- Races

## Scope
In scope:
- Seed competitions: Philip 2025, David 2025, Main 2026
- Fetch and upsert drivers from Jolpica
- Fetch and upsert races from Jolpica
- Apply placeholder deadline policy:
  - PreQualyDeadlineUtc = StartTimeUtc - 30 minutes
  - FinalDeadlineUtc = StartTimeUtc - 30 minutes
- Idempotent reruns and safe restart behavior

Out of scope:
- Persisted race results
- Standings/history
- Constructor/team normalization
- User selections and race metadata authoring flows

## Implemented In This Slice
- Added scheduled worker project at `src/F1.DataSyncWorker`.
- Added startup config validation and Postgres wiring.
- Added Jolpica HTTP client with bounded retry.
- Added idempotent competition/driver/race upsert orchestration.
- Added deterministic season-to-competition mapping validation.
- Added placeholder deadline policy implementation.
- Added run summary logging (insert/update counts).
- Added documentation and env examples for worker config.

## Story Breakdown
1. Worker Skeleton + Runtime
- Status: Complete
- Done when: Worker starts, validates config, connects to Postgres and Jolpica.

2. Competition Seeding
- Status: Complete
- Done when: Philip 2025, David 2025, Main 2026 are created/upserted with stable matching.

3. Driver Ingestion
- Status: Complete
- Done when: Driver rows are upserted by DriverId.

4. Race Ingestion + Deadline Placeholders
- Status: Complete
- Done when: Race schedule upserted and both deadlines set to 30 minutes pre-start.

5. Operational Hardening
- Status: In Progress
- Implemented: HTTP retries, summary logs, restart-safe idempotent writes.
- Remaining: Dedicated runbook rollback drills and production alert wiring.

6. Cutover/Decommission
- Status: In Progress
- Implemented: README marks legacy function path as non-canonical.
- Remaining: Infrastructure-level disable/deprioritize in deployment/runtime manifests.

## Risks and Mitigations
- Competition mapping ambiguity:
  - Mitigation: explicit season-to-competition key mapping with startup validation.
- Placeholder deadlines can be misread as final behavior:
  - Mitigation: documented as temporary policy in this epic.
- External API instability/rate limits:
  - Mitigation: retries + idempotent reruns.

## Acceptance Criteria Checklist
- Worker runs end-to-end and populates Competitions, Drivers, and Races in Postgres: Implemented.
- Running worker twice does not create duplicates for competitions/drivers/races: Implemented via deterministic upserts.
- Races link to valid competition rows: Implemented via resolved competition IDs from seeded map.
- Deadline placeholders are populated as agreed: Implemented (30 minutes before start).
- Existing web flows continue to work: No runtime behavior changed in API/web flow in this slice.
- Legacy function path is disabled/deprioritized: Deprioritized in docs; full runtime cutover pending infra changes.

## Phase 2 Follow-on Epic
- Persisted race results and standings
- Replace mocked /races/results
- Extend worker for completed race results
- Optional constructor/team ingestion model
