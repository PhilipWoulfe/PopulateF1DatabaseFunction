namespace PopulateF1Database.Data
{
    public interface IDataRepository
    {
        Task<List<dynamic>> GetItemsAsync();
    }
}
