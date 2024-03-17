using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data;

namespace Capital_and_Cargo
{
    internal class TransitManager
    {
        private SqliteConnection _connection;
        private GameDataManager dataManager;

        public TransitManager(ref SqliteConnection connection, ref GameDataManager dataManager)
        {
            _connection = connection;
            EnsureTableExistsAndIsPopulated();
            this.dataManager = dataManager;
        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("city_transit"))
            {
                CreateTransitTable();
            }
        }

        private bool TableExists(string tableName)
        {
            string sql = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                var result = command.ExecuteScalar();
                return result != null && result.ToString() == tableName;
            }
        }

        private void CreateTransitTable()
        {
            string sql = @"
            CREATE TABLE city_transit (
    TransitID INTEGER PRIMARY KEY AUTOINCREMENT,
    OriginCity TEXT NOT NULL,
    DestinationCity  TEXT NOT NULL,
    Distance REAL NOT NULL, -- Assuming distance is in kilometers
    Progress REAL NOT NULL, -- Assuming progress is a percentage (0-100)
    CargoType TEXT NOT NULL,
    CargoAmount INTEGER NOT NULL, -- Assuming cargo amount is in units or kilograms, depending on cargo type
    TransportationMethod TEXT NOT NULL,
    Price REAL NOT NULL
);
";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

       
        public DataTable LoadTransit()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT OriginCity,DestinationCity,Progress,CargoType,CargoAmount FROM city_transit;";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText= sql;
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                
            }

            return dataTable;
        }
    }
}
