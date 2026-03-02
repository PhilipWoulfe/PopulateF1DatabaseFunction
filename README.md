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
│   └── F1.Tests.Unit     # XUnit Test Suite
├── docker-compose.yml    # Infrastructure Blueprint
└── Dockerfile            # Multi-stage Docker Build
```

## 🚦 Getting Started
1. **Clone**
```bash
git clone https://github.com/PhilipWoulfe/F1Competition.git
cd F1Competition
```

2. **Configuration**
Create a Cosmos DB account and retrieve the connection string.
Update the `local.settings.json` file with the Cosmos DB connection string

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

3. **Run with Docker Compose**
The simplest way to run the API locally or on a server is via Docker Compose:
```
# Start the API and the Watchtower agent
docker compose up -d
```
4. **Manual Development**
If running locally without Docker:
```
dotnet restore
dotnet build
dotnet run --project src/F1.Api/F1.Api.csproj
```

## 📄 License

This project is licensed under the Unlicense. See the LICENSE file for details.