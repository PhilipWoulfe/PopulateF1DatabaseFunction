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
        public required string CosmosDbContainerId { get; set; }
    }

    public class JolpicaApiConfig
    {
        public required string BaseUrl { get; set; }
    }
}
