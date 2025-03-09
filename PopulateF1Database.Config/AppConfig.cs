namespace PopulateF1Database.Config
{
    public class AppConfig
    {
        public required string AzureWebJobsStorage { get; set; }
        public required string UpdateDatabaseCronSchedule { get; set; }        
        public required JolpicaApiConfig JolpicaApi { get; set; }
        public required CosmoDbConfig CosmoDb { get; set; }
        public required string Environment { get; set; } 
        public required string CompetitionYear { get; set; } 
    }

    public class CosmoDbConfig
    {
        public required string CosmosDbConnectionString { get; set; }
        public required string CosmosDbDatabaseId { get; set; }
        public required ContainersConfig Containers { get; set; }
    }

    public class ContainersConfig
    {
        public required string DriversContainer { get; set; }
        public required string PreSeasonQuestionsContainer { get; set; }
        public required string RacesContainer { get; set; }
        public required string ResultsContainer { get; set; }
        public required string SprintsContainer { get; set; }
        public required string UsersContainer { get; set; }
    }

    public class JolpicaApiConfig
    {
        public required string BaseUrl { get; set; }
    }
}
