# PXL8 Control API

**Version:** v1.0.0
**Target:** .NET 9.0
**Purpose:** Billing, admin, and policy management plane (Control Plane)

---

## 🎯 Mission

**Source of truth for billing.** Manage tenants, plans, quotas, and policies. No image delivery.

---

## 🏗️ Architecture Principles

### Control Plane Characteristics:
1. **Source of Truth**
   - PostgreSQL database (tenant, plan, quota, domain, api_key tables)
   - Billing period management (period_id as GUID)
   - Usage aggregation and invoice generation

2. **Policy Publisher**
   - Generate policy snapshots every 60s
   - Push to all Data Planes via HTTP endpoint
   - Atomic configuration bundles (tenant + quotas + domains)

3. **Budget Allocator**
   - Accept budget allocation requests from Data Planes
   - Enforce global quotas (across all Data Planes)
   - Issue TTL-based leases (5 minutes)
   - ONE active lease per (tenant, period, dataplane)

4. **Usage Processor**
   - Accept idempotent usage reports (by report_id)
   - Aggregate usage across all Data Planes
   - Track: bandwidth_used, transforms_used, storage_used

---

## 📦 What's Inside

```
Pxl8.ControlApi/
├── Controllers/
│   ├── Internal/
│   │   ├── PolicySnapshotController.cs     # GET /internal/policy-snapshot
│   │   ├── BudgetAllocationController.cs   # POST /internal/budget/allocate
│   │   └── UsageReportController.cs        # POST /internal/usage/report
│   ├── Portal/
│   │   ├── TenantsController.cs            # Tenant management (CRUD)
│   │   ├── DomainsController.cs            # Domain verification
│   │   ├── ApiKeysController.cs            # API key management
│   │   ├── QuotasController.cs             # Quota overview
│   │   └── PlansController.cs              # Plan selection
│   └── Admin/
│       ├── AdminTenantsController.cs       # Suspend/override
│       ├── AdminQuotasController.cs        # Quota adjustments
│       └── AdminReportsController.cs       # Usage reports
├── Services/
│   ├── PolicySnapshotPublisher.cs          # Generate snapshots every 60s
│   ├── BudgetAllocatorService.cs           # Allocate budget leases
│   ├── UsageAggregatorService.cs           # Process usage reports
│   └── InvoiceGeneratorService.cs          # Billing invoices
├── Data/
│   ├── Pxl8DbContext.cs                    # EF Core context
│   └── Entities/                           # Database models
│       ├── Tenant.cs
│       ├── Plan.cs
│       ├── BillingPeriod.cs
│       ├── BudgetLease.cs
│       └── UsageReport.cs
└── BackgroundServices/
    ├── PolicySnapshotPublisherWorker.cs    # Publish snapshots every 60s
    └── InvoiceGeneratorWorker.cs           # Generate invoices monthly
```

---

## 🔗 Internal API (Data Plane ↔ Control Plane)

### GET /internal/policy-snapshot
**Purpose:** Data Planes pull latest tenant policies

**Response:**
```json
{
  "snapshot_id": "guid",
  "version": 1,
  "generated_at": "2024-12-31T10:00:00Z",
  "tenants": [
    {
      "tenant_id": "guid",
      "status": "active",
      "plan_code": "pro",
      "quotas": {
        "bandwidth_limit_bytes": 107374182400,
        "transforms_limit": 1000000,
        "storage_limit_bytes": 53687091200
      },
      "domains": [
        { "domain": "example.com", "verified": true }
      ],
      "api_keys": [
        { "key_prefix": "pxl8_", "key_hmac": "base64...", "status": "active" }
      ]
    }
  ]
}
```

### POST /internal/budget/allocate
**Purpose:** Data Planes request budget leases

**Request:**
```json
{
  "request_id": "guid",  // Idempotency key
  "dataplane_id": "ru-central1-a",
  "tenant_id": "guid",
  "period_id": "guid",
  "bandwidth_requested_bytes": 10737418240,  // 10 GB
  "transforms_requested": 100000
}
```

**Response:**
```json
{
  "lease_id": "guid",
  "bandwidth_granted_bytes": 10737418240,
  "transforms_granted": 100000,
  "granted_at": "2024-12-31T10:05:00Z",
  "expires_at": "2024-12-31T10:10:00Z"  // 5 minutes TTL
}
```

### POST /internal/usage/report
**Purpose:** Data Planes submit usage reports

**Request:**
```json
{
  "report_id": "guid",  // Idempotency key
  "dataplane_id": "ru-central1-a",
  "tenant_id": "guid",
  "period_id": "guid",
  "bandwidth_used_bytes": 1073741824,  // 1 GB
  "transforms_used": 10000,
  "reported_at": "2024-12-31T10:05:30Z"
}
```

**Response:**
```json
{
  "accepted": true,
  "total_bandwidth_bytes": 5368709120,  // Aggregate across all Data Planes
  "total_transforms": 50000
}
```

---

## 🔐 Portal API (Customer Frontend)

### Endpoints:
- `GET /api/v1/tenants/me` - Current tenant info
- `GET /api/v1/quotas` - Current quota usage
- `GET /api/v1/domains` - List domains
- `POST /api/v1/domains` - Add domain
- `POST /api/v1/domains/{id}/verify` - Trigger verification
- `GET /api/v1/api-keys` - List API keys
- `POST /api/v1/api-keys` - Create API key
- `DELETE /api/v1/api-keys/{id}` - Revoke API key
- `GET /api/v1/plans` - Available plans
- `POST /api/v1/plans/upgrade` - Upgrade plan

**Authentication:** Keycloak OIDC (JWT tokens)

---

## 🛠️ Admin API

### Endpoints:
- `POST /api/v1/admin/tenants/{id}/suspend` - Suspend tenant (immediate effect)
- `POST /api/v1/admin/tenants/{id}/override-quota` - Temporary quota increase
- `GET /api/v1/admin/usage-violations` - Quota violations summary
- `GET /api/v1/admin/tenants` - List all tenants

**Authentication:** Admin role in Keycloak

---

## 🛡️ Safety Invariants

1. **One Active Lease:** UNIQUE constraint on (tenant_id, period_id, dataplane_id, status='Active')
2. **Idempotent Reports:** Usage reports deduplicated by report_id
3. **Global Quota Enforcement:** SUM(leased_budget) + requested ≤ tenant_limit
4. **Lease TTL:** Auto-expire after 5 minutes (budget recovery)
5. **Snapshot Atomicity:** All tenant configs in one transaction

---

## 📊 Observability

### Prometheus Metrics:
- `pxl8_control_budget_allocated_bytes{tenant_id}`
- `pxl8_control_usage_reported_bytes{tenant_id}`
- `pxl8_control_active_leases{tenant_id}`
- `pxl8_control_snapshot_generation_duration_seconds`

### Health Checks:
- `/health/live` - Container alive?
- `/health/ready` - Ready to serve? (DB connection OK, snapshot generator OK)

---

## 🚀 Deployment

**Target:** EU VPS (Hetzner / DigitalOcean / AWS EU)
**Infrastructure:**
- Compute: Docker container / VM
- Database: PostgreSQL (managed or self-hosted)
- Auth: Keycloak (separate container)
- Backups: Daily PostgreSQL dumps to S3

**Environment Variables:**
```bash
DATABASE_CONNECTION_STRING=Host=postgres;Database=pxl8;...
KEYCLOAK_AUTHORITY=https://auth.pxl8.io/realms/pxl8
SNAPSHOT_PUBLISH_INTERVAL=60000  # 60 seconds
DATAPLANE_ENDPOINTS=https://data1.pxl8.io,https://data2.pxl8.io
```

---

## 📝 Related Documents

- [ARCHITECTURE_SPLIT.md](../ARCHITECTURE_SPLIT.md) - Split plane architecture
- [BUDGET_ALGORITHM.md](../BUDGET_ALGORITHM.md) - Budget allocation algorithm
- [ROADMAP.md](../ROADMAP.md) - Implementation roadmap

---

**Last Updated:** 31 December 2024 (v1.0.0)
