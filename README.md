# triple-assignment-azure

Azure Functions app that starts image-generation jobs from [Buienradar](https://data.buienradar.nl/2.0/feed/json) weather stations, processes them through queues, and stores rendered images in Blob Storage. Status is tracked in Table Storage and exposed via a status endpoint.

## Flow

1. **POST** `RequestImageGeneration` → returns `202` with a `jobId` and enqueues a fan-out job.
2. **BuienradarFanout** (queue) reads the Buienradar feed, enqueues one message per station (up to 50) to the image queue, and records the job as `STARTED` in Table Storage.
3. **ProcessStationJob** (queue) fetches an image per message, overlays station name/ID, uploads to Blob Storage, and updates progress in Table Storage (and `FINISHED` when all are done).
4. **GET** `GetJobStatus?jobId=<id>` returns progress (`completed`/`total`, `status`) and time-limited SAS URLs to download the finished images.

## Running locally

- **.NET 10**, **Azure Functions Core Tools** (v4).
- Copy or create `local.settings.json` with at least:
  - `AzureWebJobsStorage` (or `TableStorageConnection` / `BlobStorageConnection`) — connection string for Storage account (queues, table, blob).
  - Optional: `TableStorage__TableName`, `BlobStorage__ContainerName`, `Buienradar__FeedUrl`.
- Run:
  ```bash
  func start
  ```
  or from repo root:
  ```bash
  dotnet build && func start 
  ```
- Base URL: `http://localhost:7071`.

## API (deployed)

Base URL: **https://assignment-for-tripple-az.azurewebsites.net**

Both endpoints require the function key as the `code` query parameter.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/RequestImageGeneration?code=<key>` | Start a new job. Returns `202` with `{ "jobId": "<guid>", "status": "STARTED" }`. |
| GET | `/api/GetJobStatus?jobId=<guid>&code=<key>` | Get job progress and image URLs. Returns `200` with `jobId`, `status`, `completed`, `total`, `outputs` (array of `{ name, url }` with SAS URLs). |

Example with curl:

```bash
# Start job
curl -X POST "https://assignment-for-tripple-az.azurewebsites.net/api/RequestImageGeneration?code=YOUR_FUNCTION_KEY"

# Get status (use jobId from response above)
curl "https://assignment-for-tripple-az.azurewebsites.net/api/GetJobStatus?jobId=YOUR_JOB_ID&code=YOUR_FUNCTION_KEY"
```

For VS Code REST Client, use the `api.http` file and set the `code` / `functionKeyRequestJob` / `functionKeyGetJob` variables to the keys from Azure Portal.




## Requirements

## ### Must

- Expose publicly accessible API for requesting a set of fresh images with current weather data using a HttpTrigger. - DONE  
- Employ QueueTrigger to process the job in the background so the initial call stays fast. - DONE  
- Employ Blob Storage to store all generated images and to expose the files. - DONE  
- Employ Buienrader api to get weather station data - DONE  
- Employ any public api for retrieving an image to write the weather data on. - DONE (used picsum)  
- Expose a publicly accessible API for fetching the generated images using HttpTriggers. - DONE (combined with could requirement, it gets the status and images links)  
- Provide HTTP files as API documentation. - DONE  
- Create a fitting Bicep template (include the queues as well). - DONE  
- Create a deploy.ps1 script that publishes your code using the dotnet cli, creates the resources in azure using the Bicep template and deploys the function using the azure cli. - DONE  
- Employ multiple queues, one for starting the job and one for fetching and updating an image. - DONE  
- Deploy the code to azure and have a working endpoint. - DONE  


## ### Could

- **SAS tokens for finished images** — Blob container is private (`PublicAccessType.None`). The status endpoint returns SAS URLs (10-minute validity) only; no public blob URLs. - DONE  

- **Build and deploy from GitHub** — GitHub Actions workflow (`.github/workflows/deploy-azure.yml`) builds and deploys on push/PR to the `az-deployment` branch using Azure Functions publish profile. - DONE (Deployed from github, please see pull request to https://github.com/BohdanZakusylo-minor/triple-assignment-azure/pull/4)  

- **Status endpoint and Table Storage** — `GetJobStatus` returns progress and blob list; job state is stored in Table Storage (STARTED, completed count, FINISHED). - DONE