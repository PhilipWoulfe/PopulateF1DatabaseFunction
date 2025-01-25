namespace PopulateF1Database.Config
{
    public class AppConfig
    {
        public required string AzureWebJobsStorage { get; set; }
        public required string CosmosDBConnectionString { get; set; }
        public required string UpdateDatabaseCronSchedule { get; set; }
        public required JolpicaApiConfig JolpicaApi { get; set; }
    }

    public class JolpicaApiConfig
    {
        public required string BaseUrl { get; set; }
    }
}
