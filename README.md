# PopulateF1Database

## Overview
PopulateF1Database is an Azure Functions project designed to interact with a Cosmos DB database and the Jolpica API. The project includes functionality to update the database on a scheduled basis using a timer trigger. The solution is built using .NET 8 and leverages dependency injection, configuration management, and HTTP client services.

## Features
- Azure Functions: Timer-triggered function to update the database.
- Cosmos DB Integration: Interacts with Cosmos DB to retrieve and store data.
- Jolpica API Integration: Fetches data from the Jolpica API.
- Configuration Management: Uses local.settings.json and AppConfig for configuration.
- Dependency Injection: Utilizes dependency injection for services and repositories.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools
- Azure Cosmos DB account
- Jolpica API access

## Getting Started

### Clone the Repository

```
git clone https://github.com/your-repo/PopulateF1Database.git
cd PopulateF1Database
```

### Configuration

1. Create a Cosmos DB account and retrieve the connection string.
2. Update the `local.settings.json` file with the Cosmos DB connection string
3. Update the `AppConfig.cs` file with the Cosmos DB database and container names.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "UpdateDatabaseCronSchedule": "0 */5 * * * *",
    "CosmosDBConnectionString": "your-cosmos-db-connection-string",
    "CosmosDBDatabaseId": "your-database-id",
    "CosmosDBContainerId": "your-container-id",
    "Environment": "Dev",
    "CompetitionYear": "2023"
  },
  "JolpicaApi": {
    "BaseUrl": "https://api.jolpi.ca/ergast/f1/"
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

```
PopulateF1Database
├── PopulateF1Database
|  ├── Functions
│  |   └── UpdateDatabase.cs
|  └── Program.cs
├── Config
│   ├── AppConfig.cs
│   └── local.settings.json
├── Data
│   ├── Interfaces
│   │   └── IDataRepository.cs
│   └── Repositories
│       └── CosmosDataRepository.cs
├── Services
│   ├── Interfaces
│   │   └── IJolpicaService.cs
│   └── Services
│       └── JolpicaService.cs
└── Tests
    └── UpdateDatabaseTests.cs
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