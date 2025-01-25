namespace PopulateF1Database.Config
{
    public class AppConfig
    {
        public string AzureWebJobsStorage { get; set; }
        public string UpdateDatabaseCronSchedule { get; set; }
        public string CosmosDBConnectionString { get; set; }
        public JolpicaApiConfig JolpicaApi { get; set; }
        public string CosmosDBDatabaseId { get; set; }
        public string CosmosDBContainerId { get; set; }
    }

    public class JolpicaApiConfig
    {
        public required string BaseUrl { get; set; }
    }
}
