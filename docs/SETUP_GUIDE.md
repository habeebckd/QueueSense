# QueueSense — Setup & Developer Quickstart Guide

This guide details how to configure, run, and develop QueueSense locally and with Docker.

---

## 1. System Prerequisites

- **.NET SDK**: .NET 9.0 (or .NET 8.0 LTS compatible)
- **Node.js**: v18.x or v20.x+ & npm / pnpm
- **PostgreSQL**: v15+ (Local instance or via Docker)
- **Docker & Docker Compose**: Recommended for containerized startup

---

## 2. Default Seed Credentials

Upon initial database migration and startup, the database is seeded with a default administrator account:
- **Email**: `admin@jobflow.ai`
- **Password**: `AdminPassword123!`
- **Role**: `Admin`

---

## 3. Environment Configuration (`.env`)

Create a `.env` file in the project root:

```env
# Database Configuration
POSTGRES_DB=jobflow
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432
POSTGRES_HOST=localhost

# Connection Strings
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=jobflow;Username=postgres;Password=postgres;Include Error Detail=true

# JWT Authentication
Jwt__Secret=SuperSecretJobFlowSecurityKeyThatIsAtLeast32BytesLong!2026
Jwt__Issuer=JobFlowApi
Jwt__Audience=JobFlowClient
Jwt__ExpirationMinutes=1440

# AI Provider Configuration
# Supported: "Mock", "OpenAI", "Ollama"
AI__Provider=Mock
AI__OpenAI__ApiKey=
AI__OpenAI__Model=gpt-4o
AI__OpenAI__BaseUrl=https://api.openai.com/v1
AI__Ollama__BaseUrl=http://localhost:11434
AI__Ollama__Model=llama3

# Worker Configuration
Worker__PollingIntervalSeconds=2
Worker__LeaseDurationMinutes=5
Worker__MaxConcurrency=4
```

---

## 4. Running with Docker Compose (Recommended)

To run the entire system (PostgreSQL database, ASP.NET Core API + Background Workers, and React Frontend) with a single command:

```bash
docker compose up -d --build
# Or using Makefile:
make up
```

> [!TIP]
> See [DOCKER_SETUP.md](DOCKER_SETUP.md) for full container topology, multi-stage build details, and Windows/PowerShell commands.

Access points:
- **Frontend Dashboard**: `http://localhost:3000`
- **Backend API & Swagger UI**: `http://localhost:5000/swagger`
- **SignalR Hub**: `http://localhost:5000/hubs/jobflow`
- **PostgreSQL**: `localhost:5432`

---

## 5. Running Locally for Development

### 5.1 Database Setup
If using a local PostgreSQL service:
```bash
# Ensure PostgreSQL is running on port 5432 and database 'jobflow' exists
createdb -U postgres jobflow
```

Or run PostgreSQL via Docker:
```bash
docker-compose up -d postgres
```

> [!IMPORTANT]
> **Docker & Hardware Virtualization Prerequisites on Windows**:
> Docker Desktop requires hardware-assisted virtualization (Intel VT-x / AMD-V) enabled in BIOS/UEFI firmware and WSL 2:
> 1. **BIOS / UEFI**: Ensure `Intel (VMX) Virtualization Technology` is set to **Enabled**.
> 2. **WSL 2 Installation** (in PowerShell as Administrator):
>    ```powershell
>    wsl --install --no-distribution
>    dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
>    dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
>    ```
> 3. **Restart the system** to apply hypervisor platform features, then start Docker Desktop.

### 5.2 Backend API & Worker (.NET)
```bash
cd src/backend/JobFlow.Api
dotnet run
```
The API will automatically execute `db.Database.Migrate()` to apply EF Core migrations and seed sample jobs on first run. Migrations can also be applied manually via:
```bash
dotnet ef database update --project ../JobFlow.Infrastructure --startup-project .
```

### 5.3 Frontend React Application
```bash
cd src/frontend
npm install
npm run dev
```
Open `http://localhost:5173` in your browser.

---

## 6. Built-in Test Scenarios Quickstart

Once logged in to the dashboard:
1. Navigate to **Job Management**.
2. Click **Run Now** on `Sample Data Processing Job` to observe live progress and logs.
3. Click **Run Now** on `Simulated Failure Job` to watch the execution fail, trigger AI root cause analysis, and display diagnosis cards.
4. Try **Retry** on the failed execution to observe attempt lineage tracking.
5. Click **Run Now** on `Configurable Long-Running Job`, then immediately click **Cancel** in the Execution Monitor to verify cooperative cancellation.
