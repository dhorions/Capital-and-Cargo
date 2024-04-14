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
        private FactoryManager factory;
        private PlayerManager player;

        private String reputationCalculation;
        public CitiesManager(ref SqliteConnection connection, ref GameDataManager dataManager,String reputationCalculation, ref PlayerManager player, ref FactoryManager factory)
        {
            _connection = connection;
            this.player = player;
            this.factory = factory;
            this.dataManager = dataManager;
            this.reputationCalculation = reputationCalculation;
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
                Country TEXT NOT NULL,
                Imported INTEGER NOT NULL DEFAULT 0,
                Exported INTEGER NOT NULL DEFAULT 0,
                Bought INTEGER NOT NULL DEFAULT 0,
                Sold INTEGER NOT NULL DEFAULT 0,
                Unlocked INTEGER default 0
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
            INSERT INTO cities (City, Latitude, Longitude, Continent, Country,Unlocked) VALUES
            
            ('Frankfurt', 50.1109, 8.6821, 'Europe', 'Germany',1),
            ('Paris', 48.8566, 2.3522, 'Europe', 'France',1),
            ('Moscow', 55.7558, 37.6173, 'Europe', 'Russia',1),
            ('Istanbul', 41.0082, 28.9784, 'Europe', 'Turkey',1),

            ('London', 51.5074, -0.1278, 'Europe', 'United Kingdom',0),

            ('Shanghai', 31.2304, 121.4737, 'Asia', 'China',0),
            ('Guangzhou', 23.1291, 113.2644, 'Asia', 'China',0),
            ('Singapore', 1.3521, 103.8198, 'Asia', 'Singapore',0),
            ('Shenzhen', 22.5431, 114.0579, 'Asia', 'China',0),
            ('Jakarta', -6.2088, 106.8456, 'Asia', 'Indonesia',0),
            ('Tokyo', 35.6895, 139.6917, 'Asia', 'Japan',0),
            ('Seoul', 37.5665, 126.9780, 'Asia', 'South Korea',0),
            ('Beijing', 39.9042, 116.4074, 'Asia', 'China',0),
            ('Mumbai', 19.0760, 72.8777, 'Asia', 'India',0),
            ('Delhi', 28.7041, 77.1025, 'Asia', 'India',0),
            ('Dubai', 25.2048, 55.2708, 'Asia', 'United Arab Emirates',0),

            ('New York City', 40.7128, -74.0060, 'North America', 'USA',0),
            ('Los Angeles', 34.0522, -118.2437, 'North America', 'USA',0),
            ('Houston', 29.7604, -95.3698, 'North America', 'USA',0),
            ('Mexico City', 19.4326, -99.1332, 'North America', 'Mexico',0),

            
            ('São Paulo', -23.5505, -46.6333, 'South America', 'Brazil',0),
            ('Buenos Aires', -34.6037, -58.3816, 'South America', 'Argentina',0),
            ('Bogota', 4.7110, -74.0721, 'South America', 'Colombia',0)
            
           
           ;
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
            string sql = $@"SELECT continent,country,city,
                case 
                    when sum(amount) is null then 0
                    else  sum(amount)
                end as Inventory,
                {reputationCalculation} as Reputation
                  
                FROM cities left join warehouse on cities.city = warehouse.CityName
                where Unlocked = 1
                group by city
                order by continent, country, city
                ";

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
                    SellPrice REAL NOT NULL,
                    UNIQUE (CityName,CargoType)
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
        public void PopulateCityMarketTable(DataTable cities, List<(String CargoType, double BasePrice, double minPrice, double maxPrice,double BaseFactoryprice, double BaseFactoryProduction)> cargoTypes)
        {
            /*DataTable market = GetGoodsForCity("Beijing");
            if(market.Rows.Count> 0)
            {
                //already populated
                return;
            }*/

            // var cities = dataManager.cities.LoadCities(); // Adjusted to get city names
            // var cargoTypes = dataManager.cargoTypes.GetAllCargoTypesAndBasePrices();
            //DeleteAllFromCityMarket();
             var random = new Random();

            foreach (DataRow city in cities.Rows)
            {
                foreach (var (CargoType, BasePrice,MinPrice, MaxPrice,  BaseFactoryprice,  BaseFactoryProduction) in cargoTypes)
                {
                    var economicModifier = random.NextDouble() * 0.4 - 0.2; // Generate a modifier between -0.2 and +0.2
                    var buyPrice = BasePrice * (1 + economicModifier);
                    var sellPrice = BasePrice * (1 + economicModifier / 2); // Assuming sell price is less affected

                    var supplyAmount = random.Next(100, 1001); // Example supply amount between 100 and 1000

                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                    INSERT OR IGNORE INTO city_market (CityName, CargoType, SupplyAmount, BuyPrice, SellPrice)
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


                    //Ensure there is some supply
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                           update city_market set SUpplyAmount = SUpplyAmount + 500 where SUpplyAmount< 100";


                        command.ExecuteNonQuery();
                    }
                    
                    //Now calculate new prices for each city

                    foreach (DataRow city in cities.Rows)
                    {
                        
                        {
                            var economicModifier = RandomDoubleBetween(0.8, 2.0);
                            var supplyModifier = RandomDoubleBetween(0.8, 1.2);
                            Debug.WriteLine($"market Update : {city["City"]}\t supplyModifier : {supplyModifier}\t economyModifier : {economicModifier}");
                            using (var command = _connection.CreateCommand())
                            {
                                command.CommandText = @"
                               UPDATE city_market
                               SET 
                                    SupplyAmount = round(SupplyAmount * @supplyModifier),
                                    BuyPrice = BuyPrice * @economicModifier,
                                    SellPrice = SellPrice * @economicModifier
                                    where cityName = @city;";

                                command.Parameters.AddWithValue("@economicModifier", economicModifier);
                                //command.Parameters.AddWithValue("@sellPriceModifier", economicModifier);
                                command.Parameters.AddWithValue("@supplyModifier", supplyModifier);
                                command.Parameters.AddWithValue("@city", city["City"]);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    //Ensure the prices are between minPrice and MaxPrice
                    DataTable badPrices = new DataTable();
                    using (var command = _connection.CreateCommand())
                    {

                        command.CommandText = @"
            SELECT cm.cityName, ct.cargoType, cm.buyPrice, cm.sellPrice, ct.basePrice, ct.minPrice, ct.maxPrice
FROM city_market AS cm
JOIN cargoTypes AS ct ON cm.CargoType = ct.cargoType
WHERE cm.buyPrice NOT BETWEEN ct.minPrice AND ct.maxPrice
   OR cm.sellPrice NOT BETWEEN ct.minPrice AND ct.maxPrice order by cm.cityName, ct.CargoType
";



                        using (var reader = command.ExecuteReader())
                        {
                            badPrices.Load(reader);
                        }
                    }
                        foreach (DataRow badPrice in badPrices.Rows)
                        {
                        //correct the price to be between min and max
                        Double buyPrice = (Double)badPrice["BuyPrice"];
                        Double sellPrice = (Double)badPrice["SellPrice"];
                        Double maxPrice = (Double)badPrice["MaxPrice"];
                        Double minPrice = (Double)badPrice["MinPrice"];
                        String city = (String)badPrice["cityName"];
                        String cargo = (String)badPrice["cargoType"];
                        (buyPrice,sellPrice ) = adjustPriceToRange(buyPrice,sellPrice,minPrice,maxPrice);
                        Debug.WriteLine("Adjusting incorrect price : " + city + "\t " + cargo + "\t" + badPrice["SellPrice"] + "\t->" + sellPrice);
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = @"
                               UPDATE city_market
                               SET 
                                    
                                    BuyPrice = @buyPrice,
                                    SellPrice = @sellPrice
                                    where cityName = @city and cargoType = @cargoType;";

                            command.Parameters.AddWithValue("@buyPrice", buyPrice);
                            command.Parameters.AddWithValue("@sellPrice", sellPrice);
                            command.Parameters.AddWithValue("@city", city);
                            command.Parameters.AddWithValue("@cargoType", cargo);

                            command.ExecuteNonQuery();
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
        private (Double,Double) adjustPriceToRange(Double buyPrice, Double sellPrice, Double minPrice, Double maxPrice)
        {
            //this can be improved to add some randomnessr instead of the actual min or max
            //we'll have to see how often this happens
            if(sellPrice < minPrice )
            {
                sellPrice =  minPrice + (minPrice * RandomDoubleBetween(.01, .05));//add some variation
               

            }
            else if(sellPrice > maxPrice )
            {
                sellPrice =  maxPrice - (maxPrice * RandomDoubleBetween(.01, .05));//add some variation

            }
            buyPrice = sellPrice - (sellPrice * RandomDoubleBetween(.02,.08));//Make the buyPrice slightly Less than the sellPrice
            return (buyPrice, sellPrice);

        }
        public DataTable GetPrices(string cityName, string cargoType)
        {
            DataTable dataTable = new DataTable();
            using (var command = _connection.CreateCommand())
            {
                // Prepare the SQL query to select goods for the specified city
                // This assumes your city_market table has a 'CityName' column and references 'cargoTypes' by 'CargoTypeID'
                command.CommandText = @"
            SELECT CargoType, SupplyAmount, BuyPrice, SellPrice
            FROM city_market 
            WHERE CityName = @CityName AND cargoType = @cargoType
            ORDER BY CargoType;";

                // Use parameters to prevent SQL injection
                command.Parameters.AddWithValue("@CityName", cityName);
                command.Parameters.AddWithValue("@cargoType", cargoType);

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
        public DataTable GetGoodsForCity(string cityName)
        {
            DataTable dataTable = new DataTable();

           
            using (var command = _connection.CreateCommand())
            {
                // Prepare the SQL query to select goods for the specified city
                // This assumes your city_market table has a 'CityName' column and references 'cargoTypes' by 'CargoTypeID'
                /*    command.CommandText = @"
                SELECT CargoType, SupplyAmount, BuyPrice, SellPrice
                FROM city_market 
                WHERE CityName = @CityName
                ORDER BY CargoType;";
                */

                command.CommandText = @"
                    SELECT cm.CargoType,
                           cm.SupplyAmount,
                           cm.BuyPrice,
                           cm.SellPrice,
                           ct.BaseFactoryPrice as [Factory Price],
                           ct.BaseFactoryProduction AS [Factory Production]
       
                    FROM city_market cm
                    JOIN cargoTypes ct ON cm.CargoType = ct.CargoType
                    WHERE cm.CityName = @CityName
                    ORDER BY cm.CargoType;
                ";

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
                        row["BuyPrice"] = Math.Round(buyPrice, 2).ToString("F2");
                    }

                    if (row["SellPrice"] != DBNull.Value)
                    {
                        decimal sellPrice = Convert.ToDecimal(row["SellPrice"]);
                        row["SellPrice"] = Math.Round(sellPrice, 2).ToString("F2");
                    }
                    if (row["Factory Price"] != DBNull.Value)
                    {
                        decimal sellPrice = Convert.ToDecimal(row["Factory Price"]);
                        row["Factory Price"] = Math.Round(sellPrice, 2).ToString("F2");
                    }
                    else
                    {

                    }

                }
            }

            return dataTable;
        }


    }

}
