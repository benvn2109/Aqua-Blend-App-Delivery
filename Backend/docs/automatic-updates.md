# Automatic Updates

## Approach

AquaBlend uses REST polling for automatic updates. This approach was selected because the application already uses a REST API and the frontend can periodically request records that have changed since its previous successful request.

The Sprint 2 implementation extends the original proof of concept to track changes to:

- Water Sources
- Scenarios
- Optimisation Results

## Endpoint

```http
GET /api/changes?since={timestamp}
```

### Example Request

```http
GET /api/changes?since=2026-08-25T03:00:00Z
```

The `since` query parameter is required and must contain a valid ISO 8601 UTC timestamp.

The endpoint checks the `CreatedAt` and `UpdatedAt` timestamps of supported entities and returns records created or updated after the supplied timestamp.

## Response

The endpoint returns:

- The timestamp supplied by the client as `requestedSince`.
- A server-generated UTC timestamp as `serverTimestamp`.
- Water Sources created or updated after the supplied timestamp.
- Scenarios created or updated after the supplied timestamp.
- Optimisation Result summaries for results created or updated after the supplied timestamp.

Optimisation Results are returned as lightweight summaries. The polling response includes:

- `id`
- `scenarioId`
- `status`
- `solvedAt`
- `receivedAt`
- `contractVersion`
- `totalCost`
- `currency`
- `createdAt`
- `updatedAt`

The full `resultJson` is not returned by the changes endpoint. The purpose of the endpoint is to notify the frontend that an Optimisation Result has changed rather than resend the complete optimisation result during every poll.

### Example Response

```json
{
  "requestedSince": "2026-08-25T03:00:00Z",
  "serverTimestamp": "2026-08-25T03:00:30Z",
  "waterSources": [],
  "scenarios": [],
  "optimisationResults": [
    {
      "id": 1,
      "scenarioId": 3,
      "status": "OPTIMAL",
      "solvedAt": "2026-08-25T02:58:00Z",
      "receivedAt": "2026-08-25T02:58:05Z",
      "contractVersion": "1.0",
      "totalCost": 12500.00,
      "currency": "AUD",
      "createdAt": "2026-08-25T02:58:05Z",
      "updatedAt": null
    }
  ]
}
```

If no records have changed since the supplied timestamp, the endpoint returns empty collections rather than `null`.

For example:

```json
{
  "requestedSince": "2026-08-25T03:00:00Z",
  "serverTimestamp": "2026-08-25T03:00:30Z",
  "waterSources": [],
  "scenarios": [],
  "optimisationResults": []
}
```

## Timestamp Handling

All timestamps used by the automatic updates endpoint are UTC.

The `since` parameter must be supplied as a UTC timestamp.

For example:

```text
2026-08-25T03:00:00Z
```

The endpoint rejects:

- A missing `since` parameter.
- An invalid timestamp.
- A timestamp that does not use UTC.

`CreatedAt` and `UpdatedAt` are automatically maintained by `AquaBlendDbContext` when entities are created or modified.

The endpoint uses these fields to determine whether a Water Source, Scenario, or Optimisation Result has changed since the previous poll.

## Frontend Polling

The frontend can retrieve automatic updates using the following workflow:

1. Store the `serverTimestamp` returned by the previous successful request.
2. Wait for the configured polling interval.
3. Send another request using the stored timestamp:

```http
GET /api/changes?since={lastServerTimestamp}
```

4. Process any returned Water Sources.
5. Process any returned Scenarios.
6. Process any returned Optimisation Result summaries.
7. If the frontend requires the full optimisation result, retrieve it using the appropriate Optimisation Result endpoint.
8. Store the new `serverTimestamp`.
9. Repeat the process.

Using the server-generated timestamp for the next request avoids relying on the frontend device's local clock.

## Example JavaScript

```javascript
let lastSuccessfulTimestamp = "2026-08-25T03:00:00Z";

async function pollForChanges() {
    const response = await fetch(
        `/api/changes?since=${encodeURIComponent(lastSuccessfulTimestamp)}`
    );

    if (!response.ok) {
        console.error("Polling failed");
        return;
    }

    const changes = await response.json();

    updateWaterSources(changes.waterSources);
    updateScenarios(changes.scenarios);
    updateOptimisationResults(changes.optimisationResults);

    lastSuccessfulTimestamp = changes.serverTimestamp;
}

setInterval(pollForChanges, 30000);
```

The polling interval shown above is 30 seconds and can be adjusted later according to frontend requirements.

## Sprint 2 Verification

The automatic updates implementation has been verified using automated tests.

The tests confirm that:

- Invalid timestamps return a bad request response.
- Changed Water Sources can be returned.
- Changed Scenarios can be returned.
- Changed Optimisation Results can be returned.
- Empty collections are returned when no matching changes exist.

The Sprint 2 test suite currently passes successfully with:

```text
Total tests: 13
Passed: 13
Failed: 0
Skipped: 0
```

## Notes

- REST polling continues to be used instead of SignalR.
- All timestamps are handled using UTC.
- The endpoint returns only records created or updated after the supplied `since` timestamp.
- `serverTimestamp` should be used by the frontend as the `since` value for the next successful polling request.
- Optimisation Results were added to automatic updates during Sprint 2.
- Optimisation Results are projected into `OptimisationResultSummaryDto` before being returned.
- `resultJson` is intentionally excluded from the automatic updates response.
- The Optimisation Result `Scenario` navigation property is not included in the polling response.
- Water Sources and Scenarios remain unchanged from the Sprint 1 implementation.