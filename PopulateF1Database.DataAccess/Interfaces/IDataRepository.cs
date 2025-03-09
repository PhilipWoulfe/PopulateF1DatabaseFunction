namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface IDataRepository
    {
        Task<List<dynamic>> GetItemsAsync();
    }
}
