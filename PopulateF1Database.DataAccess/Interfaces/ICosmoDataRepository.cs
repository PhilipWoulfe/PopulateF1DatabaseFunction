namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface ICosmoDataRepository
    {
        Task UpsertItemAsync<T>(T item) where T : class;

        Task UpsertItemsAsync<T>(IEnumerable<T> items) where T : class;
    }
}