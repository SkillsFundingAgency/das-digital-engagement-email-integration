

# Intorduction
Data Import: OData API to Staging table


## Data Import: OData -> Staging

This section documents how the application retrieves data from the external OData API and efficiently loads it into the staging database tables.

### Overview
- The pipeline pages the OData `Sends` endpoint, maps API objects to staging DTOs, and performs high-throughput inserts into SQL Server staging tables.
- Design goals: bounded memory (page-by-page), high throughput (SqlBulkCopy), predictable mapping (explicit mapping), retryable and cancellable operations.

### Key components
- `IExternalApiService` / `ExternalApiService` - HTTP client wrapper used to call the OData endpoints.
- `CampaignStagingService` - orchestration surface that determines eligible sends and coordinates import flows.
- `ODataPagedImporter` - pages the OData endpoint, deserializes pages into typed DTOs, and calls the bulk insert path per page.
- `JsonToDataTableConverter` (optional) - converts a JSON `value` array page into a `DataTable` when the DataTable path is used.
- `IBulkInserter` / `SqlBulkInserter` or `BulkInsertService` - abstraction that performs bulk writes to SQL Server. The implementation uses `SqlBulkCopy` with explicit column mappings.
- `UnitOfWork` / repositories - used for repositories that wrap `BulkInsertService` if you prefer repository-level calls.
- `ImportCampaignStagingHandler` - entry point handler that triggers the import process (calls `CampaignStagingService`).

### Flow (high level)
1. The handler invokes `CampaignStagingService` to find eligible sends to import.
2. `ODataPagedImporter` pages the OData `Sends` endpoint using `$skip` and `$top` (page size configurable).
3. Each page JSON is deserialized into `Send` DTOs (using `System.Text.Json` with `PropertyNameCaseInsensitive` and `JsonNumberHandling.AllowReadingFromString`).
4. The `Send` DTOs are mapped to staging DTOs (properties named to match destination columns) using an explicit mapper.
5. The staging DTO list for the page is handed to `IBulkInserter` / `BulkInsertService` which streams the list into SQL Server via `SqlBulkCopy`.
6. The process repeats until the OData endpoint returns fewer items than the page size.

### Configuration
- `EmailMarketingApi:ApiBaseUrl` and `EmailMarketingApi:ApiKey` - used by `ExternalApiService` to call the OData API.
- `EmailMarketingApi:PageSize` - default page size for OData paging (tune: start 1k, increase to 5k–10k depending on memory and network latency).
- Database connection is provided via `IDbConnectionFactory` / `ConnectionString:CampaignsDatabase` configuration.
- Staging table name is passed as a parameter or kept in configuration (e.g., `SendsStaging:TableName = dbo.Stg_EmailSends`).

### Mapping rules (explicit mapping)
- Project each `Send` DTO to a projection whose property names match the destination table columns, and explicitly convert/normalize types before performing the bulk insert.
- Convert ISO date strings to UTC `DateTime` for DateTime columns.
- For nullable DB columns, write `DBNull.Value` when the API value is null or empty.
- Map nested/expanded fields explicitly (e.g., `Campaign.Name` -> `CampaignName`, `SubAccount.Name` -> `Account`).

### Bulk insert behavior and tuning
- Bulk insert is performed per-page to bound memory.
- Implementation uses `SqlBulkCopy` with `SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepNulls` for speed (use only when acceptable for concurrency).
- Set `BatchSize` to page size or a tuned sub-batch size (e.g., 1k–5k).
- `BulkCopyTimeout` should be increased for large pages (e.g., 300–600 seconds).
- Optionally insert to an unindexed staging table then perform a single set-based merge to production.

### Reliability
- Use Polly retry policies around HTTP and SQL operations for transient errors. Typical strategy: 3 retries with exponential backoff.
- Respect `CancellationToken` during paging and bulk operations.
- Log page start/end, rows fetched, rows inserted, and any transient/fatal failures.

### Idempotency and deduplication
- Preferred approach: maintain `CampaignImportMetadata` to track imported `Send` IDs and exclude already-imported sends before bulk inserting.
- Alternate: rely on database unique keys and perform upserts in a downstream step.

### How to run locally
1. Ensure configuration (API base URL, API key and DB connection string) are set in `appsettings.Development.json` or user secrets.
2. Run the import handler (e.g., trigger `ImportCampaignStagingHandler.Handle()` from a test or host).
3. Start with `PageSize = 1000` and monitor logs and DB performance.

### Troubleshooting
- If inserts fail with unique constraint errors, verify mapping and deduplication strategy.
- If memory spikes, reduce `PageSize` and/or batch through smaller sub-batches.
- If inserts time out, increase `BulkCopyTimeout` and consider smaller batch sizes.

### Next improvements
- Add an explicit mapping configuration file (column -> JSON path) for exact control.
- Implement dynamic mapping by querying `INFORMATION_SCHEMA.COLUMNS` and building extraction delegates.
- Replace `DataTable` path with `IDataReader` streaming (already supported in `ObjectDataReader<T>` implementations) for lower allocations.