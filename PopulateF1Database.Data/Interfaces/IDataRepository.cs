namespace PopulateF1Database.Data.Interfaces
{
    public interface IDataRepository
    {
        Task<List<dynamic>> GetItemsAsync();
    }
}
