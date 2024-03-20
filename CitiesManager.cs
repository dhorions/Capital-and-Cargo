using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Diagnostics;

namespace Capital_and_Cargo
{
    internal class CitiesManager
    {
        private SqliteConnection _connection;
        private GameDataManager dataManager = null;
        public CitiesManager(ref SqliteConnection connection, ref GameDataManager dataManager)
        {
            _connection = connection;
            this.dataManager = dataManager;
            EnsureTableExistsAndIsPopulated();
        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("cities"))
            {
                CreateCitiesTable();
                if (!TableExists("city_market"))
                {
                    CreateCityMarketTable();
                }
                PopulateCitiesTable();
            }
            if (!TableExists("city_market_history"))
            {
                CreateCityMarketHistoryTable();
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

        private void CreateCitiesTable()
        {
            string sql = @"
            CREATE TABLE cities (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                City TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Continent TEXT NOT NULL,
                Country TEXT NOT NULL
            );";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private void PopulateCitiesTable()
        {
            DeleteAllFromCityMarket();
            string sql = @"
            INSERT INTO cities (City, Latitude, Longitude, Continent, Country) VALUES
('Shanghai', 31.2304, 121.4737, 'Asia', 'China'),
('Tokyo', 35.6895, 139.6917, 'Asia', 'Japan'),
('New York City', 40.7128, -74.0060, 'North America', 'USA'),
('Los Angeles', 34.0522, -118.2437, 'North America', 'USA'),
('Seoul', 37.5665, 126.9780, 'Asia', 'South Korea'),
('Beijing', 39.9042, 116.4074, 'Asia', 'China'),
('Mumbai', 19.0760, 72.8777, 'Asia', 'India'),
('Moscow', 55.7558, 37.6173, 'Europe/Asia', 'Russia'),
('São Paulo', -23.5505, -46.6333, 'South America', 'Brazil'),
('Guangzhou', 23.1291, 113.2644, 'Asia', 'China'),
('Delhi', 28.7041, 77.1025, 'Asia', 'India'),
('Mexico City', 19.4326, -99.1332, 'North America', 'Mexico'),
('London', 51.5074, -0.1278, 'Europe', 'United Kingdom'),
('Frankfurt', 50.1109, 8.6821, 'Europe', 'Germany'),
('Singapore', 1.3521, 103.8198, 'Asia', 'Singapore'),
('Shenzhen', 22.5431, 114.0579, 'Asia', 'China'),
('Jakarta', -6.2088, 106.8456, 'Asia', 'Indonesia'),
('Istanbul', 41.0082, 28.9784, 'Europe/Asia', 'Turkey'),
('Dubai', 25.2048, 55.2708, 'Asia', 'United Arab Emirates'),
('Houston', 29.7604, -95.3698, 'North America', 'USA');
        ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        public DataTable LoadCities()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT continent,country,city FROM cities order by continent, country, city;";

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
        public System.Collections.IList LoadCitiesList()
        {
            DataTable cities = LoadCities();
            var list = new List<String>();
            foreach (DataRow c in cities.Rows)
            {
                list.Add((string)c["City"]);
            }
            return list;
        }

        private void CreateCityMarketTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS city_market (
                    CityName TEXT NOT NULL,
                    CargoType String NOT NULL,
                    SupplyAmount INTEGER NOT NULL,
                    BuyPrice REAL NOT NULL,
                    SellPrice REAL NOT NULL
                );";
                   command.ExecuteNonQuery();
            }
        }
        private void CreateCityMarketHistoryTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS city_market_history (
                    Date TEXT NOT NULL,
                    CityName TEXT NOT NULL,
                    CargoType String NOT NULL,
                    SupplyAmount INTEGER NOT NULL,
                    BuyPrice REAL NOT NULL,
                    SellPrice REAL NOT NULL
                );";
                command.ExecuteNonQuery();
            }
        }

        public void DeleteAllFromCityMarket()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM city_market;";
                command.ExecuteNonQuery();
            }
        }
        public void PopulateCityMarketTable(DataTable cities, List<(String CargoType, double BasePrice)> cargoTypes)
        {
            // var cities = dataManager.cities.LoadCities(); // Adjusted to get city names
            // var cargoTypes = dataManager.cargoTypes.GetAllCargoTypesAndBasePrices();
            DeleteAllFromCityMarket();
             var random = new Random();

            foreach (DataRow city in cities.Rows)
            {
                foreach (var (CargoType, BasePrice) in cargoTypes)
                {
                    var economicModifier = random.NextDouble() * 0.4 - 0.2; // Generate a modifier between -0.2 and +0.2
                    var buyPrice = BasePrice * (1 + economicModifier);
                    var sellPrice = BasePrice * (1 + economicModifier / 2); // Assuming sell price is less affected

                    var supplyAmount = random.Next(100, 1001); // Example supply amount between 100 and 1000

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                    INSERT INTO city_market (CityName, CargoType, SupplyAmount, BuyPrice, SellPrice)
                    VALUES (@CityName, @CargoType, @SupplyAmount, @BuyPrice, @SellPrice);";

                        command.Parameters.AddWithValue("@CityName", city["City"]);
                        command.Parameters.AddWithValue("@CargoType", CargoType);
                        command.Parameters.AddWithValue("@SupplyAmount", supplyAmount);
                        command.Parameters.AddWithValue("@BuyPrice", buyPrice);
                        command.Parameters.AddWithValue("@SellPrice", sellPrice);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        private double RandomDoubleBetween(double minValue, double maxValue)
        {
            Random random = new Random();
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }
        public void UpdateCityMarketTable(DataTable cities, DateTime date)
        {
            DateTime lastDayOfPreviousMonth = date.AddDays(-date.Day);
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    // Now that we're in the previous month, set the day to 1 to get the first day of the previous month
                    DateTime firstDayOfPreviousMonth = new DateTime(lastDayOfPreviousMonth.Year, lastDayOfPreviousMonth.Month, 1);
                    //Move current prices to history
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO city_market_history(Date, CityName, CargoType, SupplyAmount, BuyPrice, SellPrice)
                            SELECT @HistoryDate, CityName, CargoType, SupplyAmount, BuyPrice, SellPrice
                            FROM city_market;";

                        command.Parameters.AddWithValue("@HistoryDate", firstDayOfPreviousMonth);

                        command.ExecuteNonQuery();
                    }
                    //Now calculate new prices for each city

                    foreach (DataRow city in cities.Rows)
                    {
                        
                        {
                            var economicModifier = RandomDoubleBetween(.2, 2.0);
                            var buyPriceModifier = RandomDoubleBetween(.01, .50); ;
                            var sellPriceModifier = economicModifier / 2;
                            var supplyModifier = RandomDoubleBetween(0.8, 1.2);
                            Debug.WriteLine($"market Update : {city["City"]}\t supplyModifier : {supplyModifier}\t buyPriceModifier : {buyPriceModifier}\t sellPriceModifier: {sellPriceModifier}");
                            using (var command = _connection.CreateCommand())
                            {
                                command.CommandText = @"
                               UPDATE city_market
                               SET 
                                    SupplyAmount = round(SupplyAmount * @supplyModifier),
                                    BuyPrice = (SellPrice * @sellPriceModifier)  - ((SellPrice * @sellPriceModifier) * @buyPriceModifier),
                                    SellPrice = SellPrice * @sellPriceModifier
                                    where cityName = @city;";

                                command.Parameters.AddWithValue("@buyPriceModifier", buyPriceModifier);
                                command.Parameters.AddWithValue("@sellPriceModifier", sellPriceModifier);
                                command.Parameters.AddWithValue("@supplyModifier", supplyModifier);
                                command.Parameters.AddWithValue("@city", city["City"]);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred updating the marklet: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }
        }
        public DataTable GetGoodsForCity(string cityName)
        {
            DataTable dataTable = new DataTable();

            using (var command = _connection.CreateCommand())
            {
                // Prepare the SQL query to select goods for the specified city
                // This assumes your city_market table has a 'CityName' column and references 'cargoTypes' by 'CargoTypeID'
                command.CommandText = @"
            SELECT CargoType, SupplyAmount, BuyPrice, SellPrice
            FROM city_market 
            WHERE CityName = @CityName
            ORDER BY CargoType;";

                // Use parameters to prevent SQL injection
                command.Parameters.AddWithValue("@CityName", cityName);

                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                foreach (DataRow row in dataTable.Rows)
                {
                    // Check for DBNull values to avoid formatting null data
                    if (row["BuyPrice"] != DBNull.Value)
                    {
                        decimal buyPrice = Convert.ToDecimal(row["BuyPrice"]);
                        row["BuyPrice"] = Math.Round(buyPrice, 3).ToString("F3");
                    }

                    if (row["SellPrice"] != DBNull.Value)
                    {
                        decimal sellPrice = Convert.ToDecimal(row["SellPrice"]);
                        row["SellPrice"] = Math.Round(sellPrice, 3).ToString("F3");
                    }
                }
            }

            return dataTable;
        }


    }

}
