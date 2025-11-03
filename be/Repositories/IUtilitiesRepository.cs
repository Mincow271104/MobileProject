using System.Threading.Tasks;

namespace be.Repositories
{
    public interface IUtilitiesRepository
    {
        Task<object> GetAllCodesAsync(string type);
        Task<bool> CheckValidAllCodeAsync(string type, string code);
        Task<string> GenerateIDAsync(string prefix, int digitCount, string tableName, string columnName);
    }
}