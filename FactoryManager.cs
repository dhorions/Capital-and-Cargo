using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Capital_and_Cargo
{
    internal class FactoryManager
    {
        private SqliteConnection _connection;
        private String reputationCalculation;
        private CargoTypesManager cargo;
        private PlayerManager player;
        private CitiesManager cities;
        private TransitManager transit;
        private SoundMananger soundMananger;
        private static int requiredReputationPerLevel = 500;
        private static String trueIcon = "☑";
        private static String productionCalculation = "CAST(((f.AmountProduced * f.level) * (1 + f.productionBonus / 100.0)) + 0.99999 AS INTEGER)";
        private static String upgradePriceCalculation = "BaseFactoryPrice + (BaseFactoryPrice * ((COALESCE(f.Level, 0) + 1) / 5.0))";
        //private static String upgradeReputationCalculation = $"(SELECT SUM(Level) * ({requiredReputationPerLevel} * Level / 2) FROM factories f2 WHERE f2.CityName = f.CityName) + {requiredReputationPerLevel}";
        private static String upgradeReputationCalculation = $"((SELECT SUM(Level) * ({requiredReputationPerLevel} * Level / 2) FROM factories f2 WHERE f2.CityName = f.CityName) + {requiredReputationPerLevel}) * (1+ (select level/2 from factories f3 where f3.CityName = f.CityName and f3.cargoType = f.cargoType))";
        //add comment

        public FactoryManager(ref SqliteConnection connection,String reputationCalculation, ref CargoTypesManager cargo, ref PlayerManager player,ref CitiesManager cities,ref SoundMananger sound)
        {
            _connection = connection;
            this.cargo = cargo;
            this.player = player;
            this.reputationCalculation = reputationCalculation;
            this.cities = cities;
            this.soundMananger = sound;
            EnsureTableExistsAndIsPopulated();

        }
        public void setTransitManager(TransitManager transitManager)
        {
            this.transit = transitManager;
        }
        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("Factories"))
            {
                CreateFactoryTable();
            }
        }

        private void CreateFactoryTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS factories (
                    CityName TEXT NOT NULL,
                    CargoType String NOT NULL,
                    Level INTEGER NOT NULL,
                    AmountProduced INTEGER not null,
                    productionBonus INTEGER default 0,
                    AutoSellProduced INTEGER default 0,
                    AutoSellImported INTEGER default 0,
                    AutoExport INTEGER default 0,
                    AutoExportDestination String,
                    AutoExportTreshold INTEGER default 0
                );
                CREATE INDEX IF NOT EXISTS factory_citycargo ON factories (
                    CityName,
                    CargoType
                );
                ";
                command.ExecuteNonQuery();
            }
        }
        public void buildFactory(String CityName, String CargoType)
        {
            int nextLevel = (int)getExistingFactoryLevel(CityName, CargoType) + 1;

            double requiredMoney = getRequiredMoney(CargoType, CityName,nextLevel);
            //if (factoryExists(CargoType))
            //{
                
            //}
            (Boolean canBuild,String message) = canBuildFactory(CityName, CargoType);
            if(canBuild) {
                createFactory(CityName, CargoType);
                player.pay(requiredMoney, CityName, CargoType+".factory");
            }
            else
            {
                Debug.WriteLine("Can't build factory : " + message);
            }

        }
        public void addProductionBonus(String CityName, String CargoType, int bonus)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "update factories set productionBonus = productionBonus + @bonus where CityName = @city and CargoType = @cargo";
                command.Parameters.AddWithValue("@cargo", CargoType);
                command.Parameters.AddWithValue("@city", CityName);
                command.Parameters.AddWithValue("@bonus", bonus);
                command.ExecuteNonQuery();
            }
            //Now remove the used production bonus from the player
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "update player set productionBonusPool = productionBonusPool - @bonus";
               
                command.Parameters.AddWithValue("@bonus", bonus);
                command.ExecuteNonQuery();
            }
        }
        public void setAutoExport(String CityName, String CargoType,  Boolean enabled, String targetCity,int treshold)
        {
            int enabledInt = 0;
            if (enabled) enabledInt = 1;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"update factories set AutoExport =  @enabledInt, AutoExportDestination = @targetCity, AutoExportTreshold = @treshold where CityName = @city and CargoType = @cargo";
                command.Parameters.AddWithValue("@cargo", CargoType);
                command.Parameters.AddWithValue("@city", CityName);
                command.Parameters.AddWithValue("@enabledInt", enabledInt);
                command.Parameters.AddWithValue("@targetCity", targetCity);
                command.Parameters.AddWithValue("@treshold", treshold);
                command.ExecuteNonQuery();
            }
        }
        public void setAutoSellProduction(String CityName, String CargoType, String bonusType, Boolean enabled)
        {
            int enabledInt = 0;
            if (enabled) enabledInt = 1;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"update factories set {bonusType} =  @enabledInt where CityName = @city and CargoType = @cargo";
                command.Parameters.AddWithValue("@cargo", CargoType);
                command.Parameters.AddWithValue("@city", CityName);
                command.Parameters.AddWithValue("@enabledInt", enabledInt);
                command.ExecuteNonQuery();
            }
        }

        public (Boolean canBuild,String message) canBuildFactory(String CityName, String CargoType)
        {
           
            Int64 requiredReputation = requiredReputationPerLevel;
            Int64 usedReputation = (Convert.ToInt64(Math.Floor(getExistingFactoryLevelCount(CityName))) * requiredReputationPerLevel);
            requiredReputation = usedReputation + requiredReputation;
            int nextLevel = (int)getExistingFactoryLevel(CityName, CargoType) + 1;
            double requiredMoney = getRequiredMoney(CargoType, CityName, nextLevel);
            Double Money;
            Int64 Reputation;
            (Money, Reputation) = getPlayerMoneyAndReputation(CityName);
            Boolean canUpgrade = false;
            /**
             * Check if we can upgrade/build
             * */
            DataTable factory = LoadFactories(CityName, CargoType);
            if(factory.Rows.Count == 1 )
            {
                requiredReputation = (Int64)factory.Rows[0]["Upgrade Rep"];
                requiredMoney = (Double)factory.Rows[0]["Upgrade Price"];
                if ((String)factory.Rows[0]["Can Upgrade?"] == trueIcon)
                {
                    canUpgrade = true;
                }
            }
            else
            {
                //no factory yet, just check the basics
                if (Reputation >= requiredReputation && Money >= requiredMoney)
                {
                    canUpgrade = true;
                }
            }


            
            
            
            
            
            
            String message = "";
            //if (Reputation >= requiredReputation && Money >= requiredMoney)
            if(canUpgrade)
            {
                return (true, message);
            }
            else
            {
                String formattedMoney = String.Format("{0:N0}", Money);
                String formattedRequiredMoney = String.Format("{0:N0}", requiredMoney);
                String formattedReputation = String.Format("{0:N0}", Reputation);
                String formattedRequiredReputation = String.Format("{0:N0}", requiredReputation);
                var reputationMessage = $@"";
                var moneyMessage = $@"";
                if(Reputation < requiredReputation)
                {
                    reputationMessage = $@"Your reputation in {CityName} is {formattedReputation} and you need at least {formattedRequiredReputation}.
                    You can get more reputation by importing, exporting, selling and buying goods in {CityName}.";
                }
                if (Money < requiredMoney)
                {
                    moneyMessage = $@"You have €{formattedMoney} and you need €{formattedRequiredMoney}.";
                }
                message = $@"You cannot build or upgrade a {CargoType} factory in {CityName} yet.
                {reputationMessage}
                {moneyMessage}
                ";
                return (false, message);
            }
            
        }
        public (Boolean canBuild, String message) haveFactory(String CityName, String CargoType)
        {

            string SelectSQL = @"SELECT 1  FROM factories where cargoType = @cargo and cityName = @city;";
           
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SelectSQL;
                command.Parameters.AddWithValue("@cargo", CargoType);
                command.Parameters.AddWithValue("@city", CityName);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows) return (true,"You already have a " + CargoType + " factory in " + CityName);
                }
            }
             return (false, "You don't yet have a " + CargoType + " factory in " + CityName);
        }
        private double getRequiredMoney(string CargoType, String city,int level)
        {
            
            double requiredMoney = Double.MaxValue;
            DataTable dataTable = new DataTable();
            Double levelDbl = (double)level;
            string SelectSQL = @$"SELECT  ({upgradePriceCalculation})   FROM cargoTypes 
            left join factories f 
            on f.cityName = @city and f.cargoType = cargoTypes.CargoType
            WHERE cargoTypes.CargoType = @cargoType;";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SelectSQL;
                command.Parameters.AddWithValue("@cargoType", CargoType);
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@level", levelDbl);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        requiredMoney = reader.GetDouble(0);
                    }
                }

            }
            


            return requiredMoney;
        }
        private double getExistingFactoryLevelCount(string City)
        {
            double existingLevels = 0;
            DataTable dataTable = new DataTable();
            string SelectSQL = @"SELECT 0 +sum(Level) as levels  FROM factories where cityName = @cityName;";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SelectSQL;
                command.Parameters.AddWithValue("@cityName", City);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingLevels = reader.GetDouble(0);
                        }
                    }
                } catch {
                    //no factories yet
                    existingLevels = 0;

                }

            }
            return existingLevels;
        }
        private double getExistingFactoryLevel(string City,String cargoType)
        {
            double existingLevels = 0;
            DataTable dataTable = new DataTable();
            string SelectSQL = @"SELECT level FROM factories where cityName = @cityName and cargoType = @cargoType;";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SelectSQL;
                command.Parameters.AddWithValue("@cityName", City);
                command.Parameters.AddWithValue("@cargoType", cargoType);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingLevels = reader.GetInt64(0);
                        }
                    }
                }
                catch
                {
                    //no factories yet
                    existingLevels = 0;

                }

            }
            return existingLevels;
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
        public (Double Money, Int64 reputation) getPlayerMoneyAndReputation(String cityName)
        {
           
                DataTable dataTable = new DataTable();
                string sql = $"SELECT Money, {reputationCalculation} as reputation from player inner join cities    where city = @city";

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.AddWithValue("@city", cityName);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Double Money = reader.GetDouble(0); 
                            Int64 reputation = reader.GetInt64(1);
                        return (Money, reputation);
                        }
                }

                }

                return (0,0);
            
        }
        public DataRow getFactory(String city, String cargoType)
        {
            DataTable fact = new DataTable();
            string sql = @"SELECT 
                 *
              FROM factories where cityname = @city and cargotype = @cargo;
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@cargo", cargoType);
                using (var reader = command.ExecuteReader())
                {
                    fact.Load(reader);
                }

            }
            if(fact.Rows.Count > 0) { return fact.Rows[0]; }
            return null;
            
        }
        public DataTable LoadFactories(String city)
        {
            return LoadFactories(city, "");
        }
            public DataTable LoadFactories(String city,String cargo)
        {
            DataTable dataTable = new DataTable();
            string sql =
            
               $@"
                SELECT 
                    f.CargoType AS [Resource],
                    f.CityName as City,
                    f.Level AS [Level],
                    ({productionCalculation}) AS [Weekly Prod],
                    {upgradeReputationCalculation} AS [Upgrade Rep],
                    (SELECT ({upgradePriceCalculation}) FROM cargoTypes WHERE CargoType = f.CargoType) AS [Upgrade Price],
                    CASE 
                        WHEN p.Money >= (SELECT ({upgradePriceCalculation}) FROM cargoTypes WHERE CargoType = f.CargoType)
                             AND c.Reputation >= {upgradeReputationCalculation}
                        THEN 'Yes'
                        ELSE 'No'
                    END AS [Can Upgrade?],
                    (100 + productionBonus) || '%' as Efficiency,
                       case when f.AutoExport = 1 then 'Yes' else 'No' end as [Auto Export] ,
                       case when f.AutoSellImported = 1 then 'Yes' else 'No' end as [Auto Sell Import] ,
                       case when f.AutoSellProduced = 1 then 'Yes' else 'No' end as [Auto Sell Production]
                FROM 
                    factories f
                JOIN 
                    player p
                JOIN 
                    (SELECT City, {this.reputationCalculation} AS Reputation FROM cities) c ON f.CityName = c.City
                WHERE c.City = @city
            ";
            if(cargo != "")
            {
                sql += $@" and f.CargoType = @CargoType";
            }
            
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                if (cargo != "")
                {
                    command.Parameters.AddWithValue("@CargoType", cargo);
                }
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
                foreach (DataRow factory in dataTable.Rows)
                {
                    switch(factory["Can Upgrade?"])
                    {
                        case "Yes":
                            factory["Can Upgrade?"] = trueIcon;
                            break;
                        default:
                            factory["Can Upgrade?"] = "";
                            break;
                    }
                    switch (factory["Auto Export"])
                    {
                        case "Yes":
                            factory["Auto Export"] = trueIcon;
                            break;
                        default:
                            factory["Auto Export"] = "";
                            break;
                    }
                    switch (factory["Auto Sell Import"])
                    {
                        case "Yes":
                            factory["Auto Sell Import"] = trueIcon;
                            break;
                        default:
                            factory["Auto Sell Import"] = "";
                            break;
                     }
                    switch (factory["Auto Sell Production"])
                    {
                        case "Yes":
                            factory["Auto Sell Production"] = trueIcon;
                            break;
                        default:
                            factory["Auto Sell Production"] = "";
                            break;
                    }
            }
            return dataTable;
        }
        public int getFactoryProductionBonus(String city, String cargoType)
        {
            string sql = $"SELECT (100 + productionBonus) from factories  where cityName = @city and CargoType = @cargo";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@cargo", cargoType);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        int eff = reader.GetInt16(0);
                        return eff;
                    }
                }

            }
            return 100;

        }
        public DataTable LoadAllFactories()
        {
            DataTable dataTable = new DataTable();
            string sql = @"SELECT 
                   CargoType as [Resource],
                   Level as [Factory Level],
                   AmountProduced as [Daily Production]
              FROM factories;
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }

            }
            return dataTable;
        }
        public void createFactory(String city, String cargoType) 
        {
            DataTable dataTable = new DataTable();

            string SelectSQL = @"SELECT BaseFactoryProduction FROM cargoTypes WHERE CargoType = @cargoType;";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SelectSQL;
                command.Parameters.AddWithValue("@cargoType", cargoType);

                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }

            }
            if(!factoryExists(cargoType,city))
            {
                string insertSQL = @"INSERT INTO factories (CityName, CargoType, Level, AmountProduced)
                         VALUES (@cityName, @cargoType, @level, @production);";
                int production = Convert.ToInt32(dataTable.Rows[0]["BaseFactoryProduction"]);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = insertSQL;
                    command.Parameters.AddWithValue("@cityName", city);
                    command.Parameters.AddWithValue("@cargoType", cargoType);
                    command.Parameters.AddWithValue("@level", 1);
                    command.Parameters.AddWithValue("@production", production);

                    command.ExecuteNonQuery();

                }
            }
            else
            {
                SelectSQL = "UPDATE factories SET Level = Level + 1 WHERE CargoType = @cargoType AND CityName = @CityName";
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SelectSQL;
                    command.Parameters.AddWithValue("@cargoType", cargoType);
                    command.Parameters.AddWithValue("@CityName", city);

                    command.ExecuteNonQuery();

                }
            }

        }

        private bool factoryExists(string cargoType,String city)
        {
            String sql = "SELECT CargoType FROM factories WHERE CargoType = @cargoType and cityName = @city;";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@cargoType", cargoType);
                command.Parameters.AddWithValue("@city", city);

                using (var reader = command.ExecuteReader())
                {
                    return reader.HasRows;
                }

            }
        }

        public void updateProduction()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            //Debug.WriteLine("Updating Production");
            //DataTable factories = LoadAllFactories();
            String sql = @$"
                --Update records where there is already something in the inventory of the city for this cargotype
               UPDATE warehouse
                SET 
                    Amount = Amount + (
                        SELECT 
                        --( f.AmountProduced * f.level)
                        {productionCalculation}
                        FROM factories f
                        WHERE f.CityName = warehouse.CityName AND f.CargoType = warehouse.CargoType
                    ),
                    PurchasePrice = PurchasePrice + (
                        SELECT ( {productionCalculation} * cm.BuyPrice )
                        FROM factories f
                        JOIN city_market cm ON f.CityName = cm.CityName AND f.CargoType = cm.CargoType
                        WHERE f.CityName = warehouse.CityName AND f.CargoType = warehouse.CargoType
                    )
                WHERE EXISTS (
                    SELECT 1
                    FROM factories f
                    WHERE f.CityName = warehouse.CityName AND f.CargoType = warehouse.CargoType
                );
                --Insert records where there is NOT already something in the inventory of the city for this cargotype
               INSERT INTO warehouse (CityName, CargoType, Amount, PurchasePrice)
                SELECT 
                    f.CityName, 
                    f.CargoType, 
                    {productionCalculation}, 
                    ({productionCalculation} * cm.BuyPrice ) AS PurchasePrice
                FROM factories f
                JOIN city_market cm ON f.CityName = cm.CityName AND f.CargoType = cm.CargoType
                WHERE NOT EXISTS (
                    SELECT 1 
                    FROM warehouse w
                    WHERE w.CityName = f.CityName AND w.CargoType = f.CargoType
                );
               ";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();

            }
            //Keep track of amount produced
            var sqlHistory = @$"INSERT INTO HistoryDetail (Date, City, CargoType, Production)
            SELECT 
                @date AS Date,
                f.CityName AS City,
                f.CargoType AS CargoType,
                {productionCalculation}  AS Production
            FROM factories f
            JOIN city_market cm ON f.CityName = cm.CityName AND f.CargoType = cm.CargoType
            ON CONFLICT(Date, City, CargoType) DO UPDATE SET
                Production = Production + EXCLUDED.Production;";
            DateTime firstOfMonth = player.firstOfMonth(player.getCurrentDate());
            using (var command = _connection.CreateCommand())
            {
                //Debug.WriteLine("Storing production history " + firstOfMonthDate + "\t" + totalPrice + "\t" + city + "\t" + CargoType);
                command.CommandText = sqlHistory;
                
                command.Parameters.AddWithValue("@date", firstOfMonth);
                command.ExecuteNonQuery();
            }
            //AutoSell
            String autoSellInfoSql = @$"
            SELECT
                 f.CityName AS City,
                 f.CargoType AS CargoType,
                 {productionCalculation}  AS Production
             FROM factories f
             JOIN city_market cm ON f.CityName = cm.CityName AND f.CargoType = cm.CargoType
             where f.AutoSellProduced = 1";
            var autosellTable = new DataTable();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = autoSellInfoSql;
                using (var reader = command.ExecuteReader())
                {
                    autosellTable.Load(reader);
                }
                foreach(DataRow factory in autosellTable.Rows)
                {
                    DataTable prices = cities.GetPrices((String)factory["City"], (String)factory["CargoType"]);
                    Debug.WriteLine("AutoSell Production ->\t" + (Int64)factory["Production"] + " " + (String)factory["CargoType"] + " in " + (String)factory["City"]+ " for "  + (Double)prices.Rows[0]["BuyPrice"]);
                    player.sell((String)factory["City"], (String)factory["CargoType"], (Int64)factory["Production"], (Double)prices.Rows[0]["BuyPrice"]);
                }

            }
            //Auto Export
            String autoExportInfoSql = $@"
            SELECT f.CityName,
                   f.CargoType,
                   w.Amount,
                   f.AutoExportDestination
              FROM warehouse w join factories f on w.CityName = f.CityName and w.CargoType = f.CargoType
              join cities c on f.AutoExportDestination = c.City
              where f.AutoExport = 1 and w.Amount >= f.AutoExportTreshold
            ";
            var autotransportTable = new DataTable();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = autoExportInfoSql;
                using (var reader = command.ExecuteReader())
                {
                    autotransportTable.Load(reader);
                }
                foreach (DataRow factory in autotransportTable.Rows)
                {
                    //DataTable prices = cities.GetPrices((String)factory["City"], (String)factory["CargoType"]);
                    Debug.WriteLine("AutoTransport  ->\t" + (Int64)factory["Amount"] + " " + (String)factory["CargoType"] + " from " + (String)factory["CityName"] + " to " + (String)factory["AutoExportDestination"]);
                    transit.transport("plane", (String)factory["CityName"], (String)factory["AutoExportDestination"], (String)factory["CargoType"],(Int64)factory["Amount"]);
                    soundMananger.playSound(Capital_and_Cargo.Properties.Resources.planeTransport);
                }

            }
            stopwatch.Stop();
            Debug.WriteLine($"Updating production: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
