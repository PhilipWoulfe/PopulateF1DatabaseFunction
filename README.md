# F1 Competition Platform 🏎️

[![Build and Push F1 API](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/docker-build.yaml/badge.svg)](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/docker-build.yaml)
[![CodeQL](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/codeql.yml/badge.svg)](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/codeql.yml)
![Dependabot](https://img.shields.io/badge/dependabot-enabled-025E8C?logo=dependabot&logoColor=white)
![GitHub last commit](https://img.shields.io/github/last-commit/PhilipWoulfe/F1Competition)
![Code Coverage](https://img.shields.io/badge/Code%20Coverage-50%25-yellow?style=flat)

![Deployment](https://img.shields.io/badge/Deployment-Staged-blue)

![Proxmox](https://img.shields.io/badge/Host-Proxmox_LXC-orange)

## Overview

The **F1 Competition Platform** is a modular solution designed to aggregate Formula 1 data and serve it via a modern RESTful API. Built with **.NET 8** and following **Clean Architecture** principles, it automates data ingestion from external providers and serves it through a containerized infrastructure.

It consists of two main components:

1.  **PopulateF1Database (Azure Functions)**: A background worker that interacts with the Jolpica API to fetch race data and populate a Cosmos DB database on a scheduled basis.
2.  **F1.Api (ASP.NET Core)**: A RESTful API that serves the aggregated data to clients.

The solution is built using **.NET 8** and follows **Clean Architecture** principles, leveraging dependency injection, configuration management, and containerization.

---

## 🏗️ Infrastructure & CI/CD

The platform is hosted on a local **Proxmox Virtualization Environment** using Debian 12 LXC containers.

### **The Pipeline**
1. **Continuous Integration**: GitHub Actions builds the .NET solution, executes unit tests, and enforces a code coverage gate (80% target).
2. **Registry**: Successful builds on `main` are packaged into Docker images and pushed to **GitHub Container Registry (GHCR)**.
3. **Automated Staging (`f1-test`)**: The test environment runs **Watchtower**, which automatically pulls and restarts the container whenever a new `:latest` image is detected.
4. **Production Gate (`f1-prod`)**: Deployment to production requires **Manual Approval** via GitHub Environments, ensuring a stable "human-in-the-loop" verification before live updates.

| Environment | Host IP (Internal) | Port | Deployment Logic |
| :--- | :--- | :--- | :--- |
| **Test** | `192.168.0.50` | `5000` | Automated (Watchtower) |
| **Production** | `192.168.0.51` | `5000` | Manual (Gatekeeper) |

### **Public Access (Cloudflare Tunnel)**
The solution uses **Cloudflare Tunnels** to securely expose the services without opening ports on the router.
- **Profile**: `cloud` (Enabled via `COMPOSE_PROFILES=cloud` or `--profile cloud`)
- **Token**: Managed via `TUNNEL_TOKEN` in `.env`.
- **Domains**:
  - Test: `https://f1-test.philipwoulfe.com`
  - API: `https://f1-api-test.philipwoulfe.com`

---

## 🚀 Features

- **Azure Functions**: Timer-triggered function (`UpdateDatabase`) to keep data synchronized.
- **ASP.NET Core API**: A containerized Web API for data access.
- **Persistence**: High-performance NoSQL storage via **Cosmos DB**.
- **Containerized**: Full Docker support for reproducible environments across Proxmox LXCs.

---

## 🛠️ Project Structure

```text
.
├── .github/workflows/    # CI/CD Pipelines (Build, Test, Deploy)
├── src/
│   ├── F1.Api            # ASP.NET Core Web API (Entry Point)
│   ├── F1.Core           # Domain Entities & Interfaces
│   ├── F1.Infrastructure # Database & External Integrations
│   ├── F1.Services       # Business Logic
│   └── PopulateF1...     # Azure Function Ingestion Apps
├── tests/
│   └── F1.Api.Tests      # XUnit Test Suite
│   └── F1.Web.Tests      # XUnit Test Suite
├── docker-compose.yml    # Infrastructure Blueprint
└── Dockerfile            # Multi-stage Docker Build
```

## 🚦 Local Development Guide

### 1. Prerequisites
- **.NET 8 SDK**
- **Docker & Docker Compose**
- **entr** (for live test-watching on Linux/WSL):
  ```bash
  sudo apt-get update && sudo apt-get install entr
  ```

### 2. Configuration
The solution uses two primary methods for configuration: a `.env` file for Docker Compose and `local.settings.json` for the Azure Functions.

#### A. Docker Environment (`.env`)
Create a `.env` file in the root of the project. This file controls ports, URLs, and environment settings for the local Docker containers.

```bash
# Copy the example file to create your local configuration
cp .env.example .env
```
The default values in `.env.example` are configured for a standard local setup.

#### B. Azure Function (`local.settings.json`)
This file configures the data ingestion service. You will need to update `src/PopulateF1Database/local.settings.json` with your **Cosmos DB connection string** and other specific settings.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "FUNCTIONS_INPROC_NET8_ENABLED": 1,
    "Functions:Worker:HostEndpoint": "http://127.0.0.1:5001",
    "UpdateDatabaseCronSchedule": "0 */1 * * * *",
    "Environment": "Dev",
    "CompetitionYear": "2025",
    "CosmosDbConnectionString": "your-cosmos-db-connection-string",
    "CosmosDbDatabaseId": "your-database-id",
    "CosmosDbDriversContainer": "Drivers",
    "CosmosDbPreSeasonQuestionsContainer": "PreSeasonQuestions",
    "CosmosDbRacesContainer": "Races",
    "CosmosDbResultsContainer": "Results",
    "CosmosDbSprintsContainer": "Sprints",
    "CosmosDbUsersContainer": "Users",
    "JolpicaBaseUrl": "https://api.jolpi.ca/ergast/f1/",
    "CosmosDbRetryCount": 1,
    "CosmosDbRetryTime": 30,    
    "JolpicaRateLimitDelayMs": 500
  }
}
```

### 3. Running the Application
The recommended way to run the application locally is using the provided build script. This workflow **runs tests first** and will abort the build if they fail, ensuring a stable environment.

```bash
# Make the script executable (only needs to be done once)
chmod +x build.sh
```

Development & Running the App

This project supports two primary ways to run the application on a Linux VM via Docker. Both modes utilize the F1Competition.sln but handle code execution differently.
1. Normal Mode (Testing/Standard Run)

Use this mode to run the app as a "release candidate." This uses the multi-stage Dockerfile and runs the compiled binaries. It is the closest representation of the production environment.

How to run:
./build.sh

    Behavior: Compiles the app once during the build phase.

    Environment: Defaults to Development (via docker-compose.yml) to enable Cloudflare header mocking.

    Use Case: Verifying overall system stability, UI/UX testing, and preparing for deployment.

2. Debug Mode (Active Development)

Use this mode when you need to set breakpoints, inspect variables, or use "Hot Reload." This mode mounts your source code directly into the container and runs dotnet watch.

How to run:
./build.sh --debug

    Behavior: Uses dotnet watch run to restart the app whenever you save a file on the VM.

    Debugger: Requires vsdbg (installed automatically via Dockerfile.debug).

    Use Case: Fixing bugs, developing new features, and step-through debugging.

Then ctrl+shft+d (or f5) and .Net COre Docker Attach

Troubleshooting Tips

    Container Crashing? Check the logs immediately: docker logs -f f1-local.

    Environment Mismatch? If the logs say Hosting environment: Production, the identity mock is disabled. Ensure ASPNETCORE_ENVIRONMENT=Development is set in your docker-compose.yml.

    Process Picker Empty? Ensure the C# Dev Kit and Docker extensions are installed on the Remote SSH host, not just your local machine.

    Stale DLLs? If the app is acting like old code, the build.sh script handles the clean automatically, but you can run it manually:
    find . -type d −name"bin"−o−name"obj" -exec rm -rf {} +

Once running, the services will be available at:
- **API**: `http://localhost:5000`
- **Web App**: `http://localhost:5001`

## ⚠️ Troubleshooting
### .env Changes Not Applying
Symptom: You updated .env (e.g., API_BASE_URL) but the app still uses the old value.
Cause: Docker Compose only reads .env when creating containers. Watchtower updates the image but reuses the existing container configuration. 
Fix: Manually recreate the container to apply changes: 
bash +docker-compose up -d


### Why did BLAZOR_ENVIRONMENT work but API_BASE_URL fail?
Docker "snapshots" environment variables at creation time. 
- BLAZOR_ENVIRONMENT was likely present when the container was first created. 
- API_BASE_URL was likely added/changed later. Since the container wasn't recreated, it kept the old (empty) value. 

## 📄 License

This project is licensed under the Unlicense. See the LICENSE file for details.
