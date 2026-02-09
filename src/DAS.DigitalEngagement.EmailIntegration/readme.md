sequenceDiagram
    autonumber
    participant Timer as TimerTrigger Function
    participant Config as Config (Table Storage/App Settings)
    participant DI as DI Container
    participant Import as ImportService
    participant Repo as Repository / Data Access
    participant SQL as DataMart (SQL)
    participant ExtApi as ExternalApiService
    participant EShot as e-shot REST API
    participant AI as App Insights / OpenTelemetry

    Note over Timer,AI: Startup & Scheduling
    Timer->>Config: Read EmailIntegrationSchedule (cron)
    Timer->>DI: Resolve ImportService, ExternalApiService, Repository
    Timer->>AI: Log trace: "Timer triggered"

    Note over Import,Repo: Data Retrieval & Preparation
    Timer->>Import: Start workflow()
    Import->>Repo: Get source dataset / view
    Repo->>SQL: Execute query/view (configured)
    SQL-->>Repo: Return records (paged/streamed)
    Repo-->>Import: Data rows
    Import->>Import: Map fields → DTOs (Models)
    Import->>Import: Chunk/batch data (e.g., 5 KB chunks)

    Note over Import,ExtApi: Upload to Provider
    Import->>ExtApi: Submit batch payload
    ExtApi->>EShot: POST /contacts/upload (retry up to N)
    EShot-->>ExtApi: 202 Accepted + jobId
    ExtApi-->>Import: Upload accepted (jobId)

    Note over ExtApi,EShot: Poll & Confirm
    loop Until terminal status
        ExtApi->>EShot: GET /jobs/{jobId}/status
        EShot-->>ExtApi: { status: Processing|Completed|Failed }
    end
    ExtApi-->>Import: Final status & metrics

    Note over All: Observability & Error Handling
    Import->>AI: Track metrics (batches, rows, success/failure)
    ExtApi->>AI: Track API latency, retries, HTTP result codes
    Repo->>AI: Track DB timings and row counts
    Timer->>AI: Log completion with duration

    alt Failure path
        ExtApi-->>Import: Error/Timeout
        Import->>AI: Track exception + context
        Import-->>Timer: Failure result
    else Success path
        Import-->>Timer: Success result
    end