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
    internal class PlayerManager
    {
        private SqliteConnection _connection;
     //add comment

        public PlayerManager(ref SqliteConnection connection)
        {
            _connection = connection;
            EnsureTableExistsAndIsPopulated();
           
        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("player"))
            {
                CreatePlayerTable();
                InitPlayerTable();
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

        private void CreatePlayerTable()
        {
            string sql = @"
           CREATE TABLE Player (
    Date TEXT NOT NULL,
    Money REAL NOT NULL
);
";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        public void InitPlayerTable()
        {
            

           
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                    INSERT INTO Player (Date, Money)
                    VALUES (@Date, @Money);";

                        command.Parameters.AddWithValue("@Date", "1900-01-01");
                        command.Parameters.AddWithValue("@Money", 1000000000);

                        command.ExecuteNonQuery();
                    }
               
            
        }

        public DataTable LoadPlayer()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT Date, Money from Player";

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
