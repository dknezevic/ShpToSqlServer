using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace ShpToSql.SqlConnectionControl
{
    public class SmoTasks : ISmoTasks
    {
        public IEnumerable<string> SqlServers
        {
            get
            {
                return SmoApplication
                    .EnumAvailableSqlServers()
                    .AsEnumerable()
                    .Select(r => r["Name"].ToString());
            }
        }

        public List<string> GetDatabases(SqlConnectionString connectionString)
        {
            var databases = new List<string>();

            using (var conn = new SqlConnection(connectionString.WithDatabase("master")))
            {
                conn.Open();
                var serverConnection = new ServerConnection(conn);
                var server = new Server(serverConnection);
                databases.AddRange(from Database database in server.Databases select database.Name);
            }

            return databases;
        }

        public List<DatabaseTable> GetTables(SqlConnectionString connectionString)
        {
            using (var conn = new SqlConnection(connectionString.WithDatabase("master")))
            {
                conn.Open();
                var serverConnection = new ServerConnection(conn);
                var server = new Server(serverConnection);
                return 
                    server
                    .Databases[connectionString.Database]
                    .Tables
                    .Cast<Table>()
                    .Select(t => new DatabaseTable
                                     {
                                         Name = t.Name,
                                         RowCount = t.RowCount
                                     })
                    .ToList();
            }
        }
    }
}
