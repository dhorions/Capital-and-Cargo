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
                        BasePrice REAL NOT NULL,
                        MinPrice REAL NOT NULL,
                        MaxPrice REAL NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        private void PopulateCargoTypesTable()
        {
            var cargoTypes = new (string CargoType, string Unit, double BasePrice, double MinPrice, double MaxPrice)[]
            {
                ("Electronics", "Item", 1500.0,500.0,2500.0),
                ("Automobiles", "Ton", 12000.0,6000,25000),
                ("Machinery", "Ton", 9500.0,3000,16000),
                ("Textiles", "Ton", 2000.0,500,4000),
                ("Pharmaceuticals", "Cubic Meter", 5000.0,1000,10000),
                ("Furniture", "Cubic Meter", 1300.0,200,3000),
                ("Toys", "Ton", 1500.0,230,3000),
                ("Agricultural Products", "Ton", 500.0,80,2200),
                ("Food Products", "Ton", 700.0,80,2000),
                ("Metals", "Ton", 2500.0,300,6000),
                ("Coal", "Ton", 100.0,12,1100),
                ("Cereals", "Ton", 300.0,90,1400),
                ("Plastics", "Ton", 1500.0,15,2200),
                ("Paper Products", "Ton", 800.0,5,1000),
                ("Rubber", "Ton", 2200.0,1800,3000),
                ("Wood Products", "Cubic Meter", 600.0,78,2345),
                ("Construction Materials", "Ton", 400.0,90,4000),
                ("Livestock", "Item", 250.0,3,1900),
                ("Tobacco Products", "Ton", 10000.0,8000,30000),
                ("Beverages", "Metric Ton", 600,100,3000),
                ("Footwear", "Ton", 700.0,100,2666),
                ("Glass Products", "Ton", 3000.0,1000,6000),
                ("Cosmetics", "Metric Ton", 200,90,1500),
                ("Oil & Gas", "Metric Ton", 300.0,5,7000), 
                ("Chemicals", "Metric Ton", 1200.0,600,4000), 
    
            };

            using (var transaction = _connection.BeginTransaction())
            {
                foreach (var (CargoType, Unit, BasePrice,MinPrice,MaxPrice) in cargoTypes)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO cargoTypes (CargoType, Unit, BasePrice,MinPrice,MaxPrice) VALUES (@CargoType, @Unit, @BasePrice,@MinPrice, @MaxPrice);";
                        command.Parameters.AddWithValue("@CargoType", CargoType);
                        command.Parameters.AddWithValue("@Unit", Unit);
                        command.Parameters.AddWithValue("@BasePrice", BasePrice);
                        command.Parameters.AddWithValue("@MinPrice", MinPrice);
                        command.Parameters.AddWithValue("@MaxPrice", MaxPrice);
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
        public List<(String CargoType, double BasePrice, double MinPrice, double MaxPrice)> GetAllCargoTypesAndBasePrices()
        {
            var cargoTypes = new List<(String CargoType, double BasePrice, double MinPrice, double MaxPrice)>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT CargoType, BasePrice, MinPrice, MaxPrice FROM cargoTypes;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        String cargoType = reader.GetString(0); // Assuming ID is the first column
                        double basePrice = reader.GetDouble(1); // Assuming BasePrice is the second column
                        double minPrice = reader.GetDouble(2);
                        double maxPrice = reader.GetDouble(3);
                        cargoTypes.Add((cargoType, basePrice,minPrice,maxPrice));
                    }
                }
            }

            return cargoTypes;
        }
    }
}
