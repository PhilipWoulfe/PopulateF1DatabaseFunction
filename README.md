# F1 Competition Platform

[![Build and Push F1 API](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/docker-build.yaml/badge.svg)](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/docker-build.yaml)
![CodeQL](https://github.com/PhilipWoulfe/F1Competition/actions/workflows/codeql.yml/badge.svg)
![GitHub last commit](https://img.shields.io/github/last-commit/PhilipWoulfe/F1Competition)
![Code Coverage](https://img.shields.io/badge/Code%20Coverage-50%25-yellow?style=flat)

## Overview

The **F1 Competition Platform** is a solution designed to aggregate Formula 1 data and serve it via a modern API. It consists of two main components:

1.  **PopulateF1Database (Azure Functions)**: A background worker that interacts with the Jolpica API to fetch race data and populate a Cosmos DB database on a scheduled basis.
2.  **F1.Api (ASP.NET Core)**: A RESTful API that serves the aggregated data to clients.

The solution is built using **.NET 8** and follows **Clean Architecture** principles, leveraging dependency injection, configuration management, and containerization.

## Features

- **Azure Functions**: Timer-triggered function (`UpdateDatabase`) to keep data synchronized.
- **ASP.NET Core API**: A containerized Web API for data access.
- **Cosmos DB Integration**: High-performance NoSQL storage for drivers, races, and results.
- **Jolpica API Integration**: Reliable fetching of external F1 data.
- **Docker Support**: The API is fully containerized for easy deployment.
- **CI/CD**: Automated build and test pipelines using GitHub Actions.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools
- Docker Desktop (optional, for running the API)
- Azure Cosmos DB account (or Cosmos DB Emulator)

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/PhilipWoulfe/F1Competition.git
cd F1Competition
```

### Configuration

1. Create a Cosmos DB account and retrieve the connection string.
2. Update the `local.settings.json` file with the Cosmos DB connection string

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

### Build and Run
1.	Restore Dependencies: Restore the project dependencies.
 
```
dotnet restore
```

2.	Build the Project: Build the project.
```
dotnet build
```

3.	Run the Functions: Start the Azure Functions host.
```
func start
```

## Project Structure

The solution follows a modular structure:

```text
src
├── F1.Api                          # ASP.NET Core Web API (Entry Point)
├── F1.Core                         # Domain entities and core interfaces
├── F1.Infrastructure               # Infrastructure (Database, External Services)
├── F1.Services                     # Business Logic Layer
├── PopulateF1Database              # Azure Function App (Timer Trigger)
├── PopulateF1Database.Config       # Configuration models
├── PopulateF1Database.DataAccess   # Data access implementation
├── PopulateF1Database.Models       # Data models
├── PopulateF1Database.Services     # Ingestion services
└── PopulateF1Database.Tests        # Unit tests for the Function App
tests
└── F1.Tests.Unit                   # Unit tests for the API/Core
```

## Key Components
- Config: Contains configuration classes and settings.
- Data: Contains data access interfaces and repository implementations.
- Functions: Contains Azure Functions.
- Services: Contains service interfaces and implementations for external API interactions.
- Tests: Contains unit tests for the project.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License
This project is licensed under the Unlicence License. See the LICENSE file for details.
