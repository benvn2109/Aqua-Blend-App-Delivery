# Optimisation Result API Contract

## Purpose

This document defines the API contract for optimisation results, including the
request structure, response structure, status values, validation behavior,
and common error responses.

## Endpoint

### Create Optimisation Result

**Planned endpoint**

`POST /api/optimisation-results`

This endpoint accepts a solver result associated with an existing Scenario.

### Request

```json
{
  "scenarioExternalId": "SCN-001",
  "status": "OPTIMAL",
  "solvedAt": "2026-08-05T10:00:00Z",
  "receivedAt": "2026-08-05T10:01:00Z",
  "contractVersion": "1.0",
  "resultJson": "{\"objectiveValue\":1250.50}",
  "totalCost": 1250.50,
  "currency": "USD"
}