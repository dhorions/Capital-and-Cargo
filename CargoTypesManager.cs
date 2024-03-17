using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace Capital_and_Cargo
{
    internal class CargoTypesManager
    {
        private SqliteConnection _connection;
        private GameDataManager dataManager;
        public CargoTypesManager(ref SqliteConnection connection, ref GameDataManager dataManager)
        {
            _connection = connection;
            this.dataManager = dataManager;
            EnsureTableExistsAndIsPopulated();
        }

        private void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("cargoTypes"))
            {
                CreateCargoTypesTable();
                PopulateCargoTypesTable();
            }
        }

        private bool TableExists(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                var result = command.ExecuteScalar();
                return result != null && result.ToString() == tableName;
            }
        }

        private void CreateCargoTypesTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE cargoTypes (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        CargoType TEXT NOT NULL,
                        Unit TEXT NOT NULL,
                        BasePrice REAL NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        private void PopulateCargoTypesTable()
        {
            var cargoTypes = new (string CargoType, string Unit, double BasePrice)[]
            {
                ("Electronics", "Item", 1500.0),
                ("Automobiles", "Ton", 12000.0),
                ("Machinery", "Ton", 9500.0),
                ("Textiles", "Ton", 2000.0),
                ("Pharmaceuticals", "Item", 5000.0),
                ("Furniture", "Cubic Meter", 1300.0),
                ("Toys", "Ton", 1500.0),
                ("Agricultural Products", "Ton", 500.0),
                ("Food Products", "Ton", 700.0),
                ("Metals", "Ton", 2500.0),
                ("Coal", "Ton", 100.0),
                ("Cereals", "Ton", 300.0),
                ("Plastics", "Ton", 1500.0),
                ("Paper Products", "Ton", 800.0),
                ("Rubber", "Ton", 2200.0),
                ("Wood Products", "Cubic Meter", 600.0),
                ("Construction Materials", "Ton", 400.0),
                ("Livestock", "Item", 250.0),
                ("Tobacco Products", "Ton", 10000.0),
                ("Beverages", "Liter", 1.5),
                ("Footwear", "Item", 100.0),
                ("Glass Products", "Ton", 3000.0),
                ("Cosmetics", "Item", 20.0),
                ("Oil & Gas", "Metric Ton", 300.0), // Adjusted unit and example base price
                ("Chemicals", "Metric Ton", 1200.0), // Adjusted unit and example base price
    
            };

            using (var transaction = _connection.BeginTransaction())
            {
                foreach (var (CargoType, Unit, BasePrice) in cargoTypes)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO cargoTypes (CargoType, Unit, BasePrice) VALUES (@CargoType, @Unit, @BasePrice);";
                        command.Parameters.AddWithValue("@CargoType", CargoType);
                        command.Parameters.AddWithValue("@Unit", Unit);
                        command.Parameters.AddWithValue("@BasePrice", BasePrice);
                        command.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
        }

        public DataTable LoadCargoTypes()
        {
            DataTable dataTable = new DataTable();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM cargoTypes ORDER BY CargoType;";
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        public List<(String CargoType, double BasePrice)> GetAllCargoTypesAndBasePrices()
        {
            var cargoTypes = new List<(String CargoType, double BasePrice)>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT CargoType, BasePrice FROM cargoTypes;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        String cargoType = reader.GetString(0); // Assuming ID is the first column
                        double basePrice = reader.GetDouble(1); // Assuming BasePrice is the second column

                        cargoTypes.Add((cargoType, basePrice));
                    }
                }
            }

            return cargoTypes;
        }
    }
}
