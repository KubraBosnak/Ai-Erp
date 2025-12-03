using System.Data;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiErp.API.Services
{
    public class SqlExecutorService
    {
        private readonly IDbConnection _db;

        public SqlExecutorService(IDbConnection db)
        {
            _db = db; // Dapper, buradaki bağlantıyı kullanacak
        }

        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string sqlQuery)
        {
            // Dapper'ın sorguyu çalıştırdığı kritik nokta.
            return await _db.QueryAsync<dynamic>(sqlQuery);
        }
    }
}