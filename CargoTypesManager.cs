using Microsoft.Data.Sqlite;
using System;
using System.Configuration;
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
                        MaxPrice REAL NOT NULL,
                        BaseFactoryPrice REAL NOT NULL,
                        BaseFactoryProduction REAL NOT NULL,
                        Unlocked INTEGER default 0
                    );";
                command.ExecuteNonQuery();
            }
        }

        private void PopulateCargoTypesTable()
        {
            var cargoTypes = new (string CargoType, string Unit, double BasePrice, double MinPrice, double MaxPrice,Double BaseFactoryPrice, Double BaseFactoryProduction, Int64 Unlocked)[]
            {
               /* ("Electronics",     "Item", 1500.0,500.0,2500.0),
                ("Automobiles",     "Ton", 12000.0,6000,25000),
                ("Machinery",       "Ton", 9500.0,3000,16000),
                ("Textiles",        "Ton", 2000.0,500,4000),
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
    */
              /*  ("Coal", "Ton", 100.0, 12, 1100,50000,10),
                ("Cosmetics", "Metric Ton", 200, 90, 1500,50000,10),
                ("Livestock", "Item", 250.0, 3, 1900,50000,10),
                ("Cereals", "Ton", 300.0, 90, 1400,50000,10),
                ("Oil & Gas", "Metric Ton", 300.0, 5, 7000,50000,10),
                ("Construction Materials", "Ton", 400.0, 90, 4000,75000,7),
                ("Agricultural Products", "Ton", 500.0, 80, 2200,75000,7),
                ("Wood Products", "Cubic Meter", 600.0, 78, 2345,75000,7),
                ("Beverages", "Metric Ton", 600, 100, 3000,75000,7),
                ("Food Products", "Ton", 700.0, 80, 2000,75000,7),
                ("Footwear", "Ton", 700.0, 100, 2666,100000,5),
                ("Paper Products", "Ton", 800.0, 5, 1000,100000,5),
                ("Chemicals", "Metric Ton", 1200.0, 600, 4000,100000,5),
                ("Furniture", "Cubic Meter", 1300.0, 200, 3000,100000,5),
                ("Electronics", "Item", 1500.0, 500.0, 2500.0,100000,5),
                ("Toys", "Ton", 1500.0, 230, 3000,150000,2),
                ("Plastics", "Ton", 1500.0, 15, 2200,150000,2),
                ("Textiles", "Ton", 2000.0, 500, 4000,150000,2),
                ("Rubber", "Ton", 2200.0, 1800, 3000,150000,2),
                ("Metals", "Ton", 2500.0, 300, 6000,150000,2),
                ("Glass Products", "Ton", 3000.0, 1000, 6000,200000,1),
                ("Pharmaceuticals", "Cubic Meter", 5000.0, 1000, 10000,200000,1),
                ("Machinery", "Ton", 9500.0, 3000, 16000,250000,1),
                ("Tobacco Products", "Ton", 10000.0, 8000, 30000,250000,1),
                ("Automobiles", "Ton", 12000.0, 6000, 25000,300000,1)*/
              //With Unlocked status
                ("Paper Products", "Ton", 800.0, 5, 1000, 100000, 5,1),
                ("Coal", "Ton", 100.0, 12, 1100, 50000, 10,1),
                ("Cereals", "Ton", 300.0, 90, 1400, 50000, 10,1),
                ("Cosmetics", "Metric Ton", 200, 90, 1500, 50000, 10,1),
                ("Livestock", "Item", 250.0, 3, 1900, 50000, 10,1),
                ("Food Products", "Ton", 700.0, 80, 2000, 75000, 7,0),
                ("Agricultural Products", "Ton", 500.0, 80, 2200, 75000, 7,0),
                ("Plastics", "Ton", 1500.0, 15, 2200, 150000, 2,0),
                ("Electronics", "Item", 1500.0, 500.0, 2500.0, 100000, 5,0),
                ("Footwear", "Ton", 700.0, 100, 2666, 100000, 5,0),
                ("Wood Products", "Cubic Meter", 600.0, 78, 2345, 75000, 7,0),
                ("Rubber", "Ton", 2200.0, 1800, 3000, 150000, 2,0),
                ("Beverages", "Metric Ton", 600, 100, 3000, 75000, 7,0),
                ("Furniture", "Cubic Meter", 1300.0, 200, 3000, 100000, 5,0),
                ("Toys", "Ton", 1500.0, 230, 3000, 150000, 2,0),
                ("Oil & Gas", "Metric Ton", 300.0, 5, 7000, 50000, 10,0),
                ("Construction Materials", "Ton", 400.0, 90, 4000, 75000, 7,0),
                ("Textiles", "Ton", 2000.0, 500, 4000, 150000, 2,0),
                ("Chemicals", "Metric Ton", 1200.0, 600, 4000, 100000, 5,0),
                ("Metals", "Ton", 2500.0, 300, 6000, 150000, 2,0),
                ("Glass Products", "Ton", 3000.0, 1000, 6000, 200000, 1,0),
                ("Machinery", "Ton", 9500.0, 3000, 16000, 250000, 1,0),
                ("Pharmaceuticals", "Cubic Meter", 5000.0, 1000, 10000, 200000, 1,0),
                ("Automobiles", "Ton", 12000.0, 6000, 25000, 300000, 1,0),
                ("Tobacco Products", "Ton", 10000.0, 8000, 30000, 250000, 1,0)

            };

            using (var transaction = _connection.BeginTransaction())
            {
                foreach (var (CargoType, Unit, BasePrice,MinPrice,MaxPrice,  BaseFactoryPrice,  BaseFactoryProduction, Unlocked) in cargoTypes)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO cargoTypes (CargoType, Unit, BasePrice,MinPrice,MaxPrice,BaseFactoryPrice,  BaseFactoryProduction, Unlocked) VALUES " +
                            "(" +
                                "@CargoType, " +
                                "@Unit, " +
                                "@BasePrice," +
                                "@MinPrice, " +
                                "@MaxPrice," +
                                "@BaseFactoryPrice,  " +
                                "@BaseFactoryProduction," +
                                "@Unlocked" +
                            ");";
                        command.Parameters.AddWithValue("@CargoType", CargoType);
                        command.Parameters.AddWithValue("@Unit", Unit);
                        command.Parameters.AddWithValue("@BasePrice", BasePrice);
                        command.Parameters.AddWithValue("@MinPrice", MinPrice);
                        command.Parameters.AddWithValue("@MaxPrice", MaxPrice);
                        command.Parameters.AddWithValue("@BaseFactoryPrice", BaseFactoryPrice);
                        command.Parameters.AddWithValue("@BaseFactoryProduction", BaseFactoryProduction);
                        command.Parameters.AddWithValue("@Unlocked", Unlocked);
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
                command.CommandText = "SELECT * FROM cargoTypes where Unlocked = 1 ORDER BY CargoType ;";
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }
        public List<(String CargoType, double BasePrice, double MinPrice, double MaxPrice, double BaseFactoryPrice, Double BaseFactoryProduction)> GetAllCargoTypesAndBasePrices()
        {
            var cargoTypes = new List<(String CargoType, double BasePrice, double MinPrice, double MaxPrice,double BaseFactoryPrice, Double BaseFactoryProduction)>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT CargoType, BasePrice, MinPrice, MaxPrice,BaseFactoryPrice,BaseFactoryProduction  FROM cargoTypes  where Unlocked = 1;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        String cargoType = reader.GetString(0); // Assuming ID is the first column
                        double basePrice = reader.GetDouble(1); // Assuming BasePrice is the second column
                        double minPrice = reader.GetDouble(2);
                        double maxPrice = reader.GetDouble(3);
                        double BaseFactoryPrice = reader.GetDouble(4);
                        double BaseFactoryProduction = reader.GetDouble(5);
                        cargoTypes.Add((cargoType, basePrice, minPrice, maxPrice, BaseFactoryPrice, BaseFactoryProduction)); ;
                    }
                }
            }

            return cargoTypes;
        }
    }
}
