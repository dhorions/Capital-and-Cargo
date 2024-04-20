using Microsoft.Data.Sqlite;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace Capital_and_Cargo
{
    internal class AchievementManager
    {
        private SqliteConnection _connection;
        private String reputationCalculation;
        private CitiesManager cities;
        private CargoTypesManager cargoTypes;
        public AchievementManager(ref SqliteConnection connection, String reputationCalculation,ref CitiesManager cities,ref CargoTypesManager cargoTypes)
        {
            _connection = connection;
            this.reputationCalculation = reputationCalculation;
            this.cities = cities;
            this.cargoTypes = cargoTypes;
            EnsureTableExistsAndIsPopulated();
        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("achievements"))
            {
                CreateAchievementsTable();
            }
            createPlayerAchievements();
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

        private void CreateAchievementsTable()
        {
            string sql = @"
            CREATE TABLE achievements (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT UNIQUE NOT NULL,
    Path TEXT NOT NULL,
    Name  TEXT NOT NULL,
    Text  TEXT NOT NULL,
    RewardText TEXT,
    Target  INTEGER NOT NULL,
    checkSQL  TEXT NOT NULL,
    updateSql TEXT,
    achieved INT NOT NULL DEFAULT 0,
    achievedDate TEXT
);
";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        private void InsertAchievement(string key, string path, string name, string text, string rewardText, int target, string checkSQL, string updateSql)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"INSERT OR IGNORE INTO achievements (Key, Path, Name, Text, RewardText, Target, checkSQL, updateSql) VALUES (@Key, @Path, @Name, @Text, @RewardText, @Target, @checkSQL, @updateSql);";
                command.Parameters.AddWithValue("@Key", key);
                command.Parameters.AddWithValue("@Path", path);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Text", text);
                command.Parameters.AddWithValue("@RewardText", rewardText ?? string.Empty); // Handling possible null values
                command.Parameters.AddWithValue("@Target", target);
                command.Parameters.AddWithValue("@checkSQL", checkSQL);
                command.Parameters.AddWithValue("@updateSql", updateSql ?? string.Empty); // Handling possible null values

                command.ExecuteNonQuery();
            }
        }
        private void createPlayerAchievements()
        {
            //Improvement sql
            //-- Production bonus
            String prodBonusSql = "UPDATE Player SET productionBonusPool = productionBonusPool + {bonus}";
            //-- Unlock City
            String cityUnlockSql = "UPDATE cities SET Unlocked = 1 where city =  '{city}'";
            //-- Unlock CargoType
            String cargoUnlockSql = "UPDATE cargoTypes SET Unlocked = 1 where CargoType =  '{CargoType}'";
            //--Unlock AutoSellProduced in city
            String UnlockAutoSellProducedSql = "UPDATE cities SET AutoSellProducedUnlocked = 1 where  city = '{city}'";
            //--Unlock AutoSellImported in city
            String UnlockAutoSellImportedSql = "UPDATE cities SET AutoSellImportedUnlocked = 1 where city = '{city}'";
            //--Unlock AutoExportUnlocked in city
            String UnlockAutoExportUnlockedSql = "UPDATE cities SET AutoExportUnlocked = 1 where city = '{city}'";

            /*
             * ----- CITIES -----
              default : 
                Paris
                Frankfurt
                Moscow
                Istanbul
               
            * achivements created
                London

                
                Los Angeles
                Houston
                Mexico City
                New York City
                Buenos Aires
            * No achievements yet
                
                São Paulo
                Bogota
                Shanghai
                Guangzhou
                Shenzhen
                Beijing
                Mumbai
                Delhi
                Jakarta
                Tokyo
                Singapore
                Seoul
                Dubai
             * */

            /*
            *  ----- GOODS -----
           default : 
                Paper Products
                Coal
                Cereals
                Cosmetics
                Livestock
            * achievements created
                 Food Products
            * No Achievements Yet
               
                Agricultural Products
                Plastics
                Electronics
                Footwear
                Wood Products
                Rubber
                Beverages
                Furniture
                Toys
                Oil & Gas
                Construction Materials
                Textiles
                Chemicals
                Metals
                Glass Products
                Machinery
                Pharmaceuticals
                Automobiles
                Tobacco Products

            */

            //Reputation in a single city
            String sql_rep_any = "SELECT MIN(sum(" + reputationCalculation + "),{target}) as target FROM cities group by city order by target desc limit 1";
            InsertAchievement("rep/any/0000500", "rep/any", "Trade Pioneer", "Achieve {target} reputation in any city.", "Factory Building Unlocked", 500, sql_rep_any, "");
            InsertAchievement("rep/any/0005000", "rep/any", "Master Trader", "Achieve {target} reputation in any city.", "Production Bonus Points +10", 5000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}","10"));
            InsertAchievement("rep/any/0250000", "rep/any", "Local Kingpin", "Achieve {target} reputation in any city.", "Production Bonus Points +20", 250000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("rep/any/1000000", "rep/any", "City Magnate ", "Achieve {target} reputation in any city.", "Production Bonus Points +50", 1000000, sql_rep_any,  SubstitutePlaceholder(prodBonusSql, "{bonus}", "50"));
            //Total Import per month
            String sql_import_total = "  SELECT MIN(sum(Import),{target}) as target FROM HistoryDetail group by Date order by target desc limit 1";
            InsertAchievement("imp/any/month/0000500", "imp/any/month", "New Importer on the Dock", "Import {target} goods in 1 month.", "Unlock new city : London", 500, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "London"));
            InsertAchievement("imp/any/month/0005000", "imp/any/month", "Container Rookie", "Import {target} goods in 1 month.", "Unlock new city : Los Angeles", 5000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Los Angeles"));
            InsertAchievement("imp/any/month/0250000", "imp/any/month", "Freight Forwarder", "Import {target} goods in 1 month.", "Unlock new city : Houston", 250000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Houston"));
            InsertAchievement("imp/any/month/1000000", "imp/any/month", "Import Mogul", "Import {target} goods in 1 month.", "Unlock new city : Mexico City", 1000000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Mexico City"));
            //Total Expoprt per month
            String sql_export_total = "  SELECT MIN(sum(Export),{target}) as target FROM HistoryDetail group by Date order by target desc limit 1";
            InsertAchievement("imp/any/month/0000500", "imp/any/month", "First-Time Exporter", "Export {target} goods in 1 month.", "Unlock new cargo type : Food products", 500, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{CargoType}", "Food products"));
            InsertAchievement("imp/any/month/0005000", "imp/any/month", "Export Enthusiast", "Export {target} goods in 1 month.", "Unlock new cargo type : Agricultural Products", 5000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{CargoType}", "Agricultural Products"));
            InsertAchievement("imp/any/month/0250000", "imp/any/month", "Captain of Commerce", "Export {target} goods in 1 month.", "Unlock new city : New York City", 250000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "New York City"));
            InsertAchievement("imp/any/month/1000000", "imp/any/month", "Continental Connector", "Export {target} goods in 1 month.", "Unlock new city : Buenos Aires", 1000000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Buenos Aires"));
            //Total Import in a city
            String sql_import_city = "  SELECT MIN(sum(Import),{target}) as target FROM HistoryDetail group by city order by target desc limit 1";
            InsertAchievement("imp/any/0000500", "imp/any", "Market Pioneer", "Import {target} goods into a city.", "", 500, sql_import_city, "");
            InsertAchievement("imp/any/0005000", "imp/any", "Urban Supplier", "Import {target} goods into a city.", "", 5000, sql_import_city, "");
            InsertAchievement("imp/any/0250000", "imp/any", "City Stocker", "Import {target} goods into a city.", "", 250000, sql_import_city, "");
            InsertAchievement("imp/any/1000000", "imp/any", "Import Icon", "Import {target} goods into a city.", "", 1000000, sql_import_city, "");
            //Total Export from a city
            String sql_export_city = "  SELECT MIN(sum(Export),{target}) as target FROM HistoryDetail group by city order by target desc limit 1";
            InsertAchievement("exp/any/0000500", "exp/any", "Exporter Initiate", "Export {target} goods from a city.", "", 500, sql_export_city, "");
            InsertAchievement("exp/any/0005000", "exp/any", "City Export Champion", "Export {target} goods from a city.", "", 5000, sql_export_city, "");
            InsertAchievement("exp/any/0250000", "exp/any", "Metropolitan Merchant", "Export {target} goods from a city.", "", 250000, sql_export_city, "");
            InsertAchievement("exp/any/1000000", "exp/any", "Global Gateway Guru", "Export {target} goods from a city.", "", 1000000, sql_export_city, "");
            //Unlock Auto Sell, Auto Export etc per city
            String sql_autosellproduced_city = "  SELECT MIN(sum(Production),{target}) as target FROM HistoryDetail where city = '{city}' ";
            String sql_autosellimported_city = "  SELECT MIN(sum(Import),{target}) as target FROM HistoryDetail where city = '{city}' ";
            String sql_autotransport_city = "  SELECT MIN(sum(Export),{target}) as target FROM HistoryDetail where city = '{city}' ";
            DataTable cargos = cargoTypes.LoadCargoTypes();
            foreach (String city in cities.LoadCitiesList())
            {
                
                    
                    //Unlock AutoSell Produced
                    String checkSql = SubstitutePlaceholder(sql_autosellproduced_city, "{city}", city);
                    String updateSql = SubstitutePlaceholder(UnlockAutoSellProducedSql, "{city}", city);
                    InsertAchievement("autosellprod/"+ city+"/0_prod", "autosellprod/" + city, cityProductionAchievementName(city), "Produce {target}  goods in " + city, "Unlocks Auto Sell Produced Goods in "+ city, 1000, checkSql, updateSql);
                    //Unlock AutoSell Imported
                    checkSql = SubstitutePlaceholder(sql_autosellimported_city, "{city}", city);
                    updateSql = SubstitutePlaceholder(UnlockAutoSellImportedSql, "{city}", city);
                    InsertAchievement("autosellimp/" + city + "/1_imp", "autosellimp/" + city, cityImportAchievementName(city), "Import {target}  goods in " + city, "Unlocks Auto Sell Imported Goods in " + city, 1000, checkSql, updateSql);
                    //Unlock Auto Export 
                    checkSql = SubstitutePlaceholder(sql_autotransport_city, "{city}", city);
                    updateSql = SubstitutePlaceholder(UnlockAutoExportUnlockedSql, "{city}", city);
                    InsertAchievement("autotransp/" + city + "/0_autotrans", "autotransp/" + city, cityExportAchievementName(city), "Export {target}  goods from " + city, "Unlocks Auto Transport Goods in " + city, 1000, checkSql, updateSql);

            }

        }
        private String cityProductionAchievementName(String city)
        {
            Dictionary<string, string> achievements = new Dictionary<string, string>()
        {
            {"Frankfurt", "Financial Fabricator"},
            {"Paris", "Parisian Producer"},
            {"Moscow", "Moscow Manufacturer"},
            {"Istanbul", "Bosphorus Builder"},
            {"London", "London's Loom Lord"},
            {"Shanghai", "Shanghai Shipwright"},
            {"Guangzhou", "Guangzhou Gear Maker"},
            {"Singapore", "Singapore Synthesizer"},
            {"Shenzhen", "Shenzhen Silicon Smith"},
            {"Jakarta", "Jakarta Java Giant"},
            {"Tokyo", "Tech Titan of Tokyo"},
            {"Seoul", "Seoul Circuit Setter"},
            {"Beijing", "Beijing Builder"},
            {"Mumbai", "Mumbai Machinist"},
            {"Delhi", "Delhi Developer"},
            {"Dubai", "Dubai Dynamo"},
            {"New York City", "New York Networker"},
            {"Los Angeles", "LA Luminary"},
            {"Houston", "Houston Heavy Industries"},
            {"Mexico City", "Mexico City Maker"},
            {"São Paulo", "São Paulo Station"},
            {"Buenos Aires", "Buenos Aires Builder"},
            {"Bogota", "Bogota Barricade Builder"}
        };
            List<string> generalAchievements = new List<string>()
        {
            "Master Manufacturer",
            "Production Pioneer",
            "Factory Foreman",
            "Industrial Innovator",
            "Assembly Ace",
            "Manufacturing Mogul",
            "Production Prodigy",
            "Craftsmanship King",
            "Industry Icon",
            "Epic Producer",
            "Production Wizard",
            "Factory Phenom",
            "Industrial Oracle",
            "Manufacturing Mastermind",
            "Workshop Warrior"
        };
            if (achievements.ContainsKey(city))
            {
                return achievements[city];
            }
            else
            {
                Random random = new Random();
                return generalAchievements[random.Next(generalAchievements.Count)];
            }
        }
        private String cityImportAchievementName(String city)
        {
            Dictionary<string, string> achievements = new Dictionary<string, string>()
        {
            {"Frankfurt", "Euro Stock Stacker"},
            {"Paris", "Chic Supplier"},
            {"Moscow", "Red Square Retailer"},
            {"Istanbul", "Bazaar Boss"},
            {"London", "Big Ben Broker"},
            {"Shanghai", "Dragon Port Master"},
            {"Guangzhou", "Canton King"},
            {"Singapore", "Merlion Merchant"},
            {"Shenzhen", "Silicon Shipper"},
            {"Jakarta", "Archipelago Importer"},
            {"Tokyo", "Gadget Guru"},
            {"Seoul", "Kimchi Importer"},
            {"Beijing", "Forbidden City Filler"},
            {"Mumbai", "Bollywood Backer"},
            {"Delhi", "Delhi Sultan of Spice"},
            {"Dubai", "Desert Trade Sheikh"},
            {"New York City", "Statue of Liberty Loader"},
            {"Los Angeles", "Hollywood Handler"},
            {"Houston", "Oil Empire Operator"},
            {"Mexico City", "Aztec Trader"},
            {"São Paulo", "Carnival Conductor"},
            {"Buenos Aires", "Pampas Provider"},
            {"Bogota", "Andean Importer"}
        };
            List<string> generalAchievements = new List<string>()
        {
            "Global Trader",
            "Heavy Hauler",
            "Import Mogul",
            "Wholesale Wizard",
            "Container King",
            "Economic Giant",
            "Cargo Czar",
            "Bulk Buyer",
            "Trade Route Ruler",
            "Freight Tycoon",
            "Import Impresario",
            "Supply Chain Sultan",
            "Worldwide Wholesaler",
            "Mass Market Master",
            "Goods Guru"
        };
            if (achievements.ContainsKey(city))
            {
                return achievements[city];
            }
            else
            {
                Random random = new Random();
                return generalAchievements[random.Next(generalAchievements.Count)];
            }
        }
        private String cityExportAchievementName(String city)
        {
            Dictionary<string, string> achievements = new Dictionary<string, string>()
        {
            {"Frankfurt", "Finance Forwarder"},
            {"Paris", "Perfume Export Elite"},
            {"Moscow", "Matryoshka Mover"},
            {"Istanbul", "Istanbul Textile Tycoon"},
            {"London", "Tea Trade Tsar"},
            {"Shanghai", "Silk Road Reviver"},
            {"Guangzhou", "Guangzhou Industrial Exporter"},
            {"Singapore", "Singapore Spice Shipper"},
            {"Shenzhen", "Shenzhen Tech Trafficker"},
            {"Jakarta", "Jakarta Java Juggler"},
            {"Tokyo", "Tokyo Tech Trailblazer"},
            {"Seoul", "Seoul Semiconductor Supplier"},
            {"Beijing", "Beijing Business Booster"},
            {"Mumbai", "Mumbai Maritime Merchant"},
            {"Delhi", "Delhi Textile Distributor"},
            {"Dubai", "Dubai Gold Glider"},
            {"New York City", "New York Navigator"},
            {"Los Angeles", "LA Entertainment Exporter"},
            {"Houston", "Houston Oil Outfitter"},
            {"Mexico City", "Mexico City Manufacturing Maestro"},
            {"São Paulo", "São Paulo Sugar Shuffler"},
            {"Buenos Aires", "Buenos Aires Beef Baron"},
            {"Bogota", "Bogota Bloom Broker"}
        };
            List<string> generalAchievements = new List<string>()
        {
            "Global Goods Guru",
            "Export Emperor",
            "Shipping Savant",
            "World Trader",
            "Transnational Tycoon",
            "Export Expert",
            "Overseas Operator",
            "International Dealer",
            "Cross-continental Captain",
            "Merchandise Mogul",
            "Trade Trailblazer",
            "Foreign Market Maven",
            "Export Entrepreneur",
            "Export Ace",
            "Outbound Oracle"
        };
            if (achievements.ContainsKey(city))
            {
                return achievements[city];
            }
            else
            {
                Random random = new Random();
                return generalAchievements[random.Next(generalAchievements.Count)];
            }
        }
        public (Int64,Int64) getAchievementStatus()
        {
            Int64 achieved = 0;
            Int64 total = 0;
            using (var command = _connection.CreateCommand())
            {

                command.CommandText = "SELECT (select count(*) from achievements ) as total,\r\n(select count(*) from achievements where achieved = 1) as achieved";

                Double result = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        total = reader.GetInt64(0);
                        achieved = reader.GetInt64(1);
                    }
                }
            }
            return(achieved, total);

        }
        public void checkAchievements(DateTime currentDate)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            DataTable dataTable = new DataTable();

            using (var command = _connection.CreateCommand())
            {

                command.CommandText = @"select * from achievements where achieved = 0";


                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                foreach (DataRow row in dataTable.Rows)
                {
                    row["checkSQL"] = SubstitutePlaceholder((String)row["checkSQL"], "{target}", "" + row["target"]);
                    row["Text"] = SubstitutePlaceholder((String)row["Text"], "{target}", "" + row["target"]);
                    checkAchievement(row,currentDate);
                }
            }
            stopwatch.Stop();
            Debug.WriteLine($"Checking Achievements - {stopwatch.ElapsedMilliseconds} ms");

        }
        private void checkAchievement(DataRow row, DateTime currentDate)
        {
            using (var command = _connection.CreateCommand())
            {
                
                command.CommandText = (String)row["checkSQL"];
                Boolean achievementApplied = false;
                Double result = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if(!reader.IsDBNull(0))
                        { 
                            result = reader.GetInt64(0);
                            if (result >= (Int64)row["Target"])
                            {
                                Debug.WriteLine("achievement unlocked " + row["Name"] + " " + row["Text"]);
                                using (var updcommand = _connection.CreateCommand())
                                {

                                    updcommand.CommandText = "UPDATE achievements SET achieved = 1, achievedDate = @achievedDate WHERE ID = @id";
                                    updcommand.Parameters.AddWithValue("@achievedDate", currentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    updcommand.Parameters.AddWithValue("@id", row["id"]);
                                    updcommand.ExecuteNonQuery();
                                }
                                //Apply achievement
                                applyAchievement((String)row["updateSql"],(String)row["RewardText"]);
                                achievementApplied = true;
                            }
                        }
                    }
                }
                if(achievementApplied) {
                    //There maybe be things unlocked that lead to new achievements
                    createPlayerAchievements();
                }



            }


        }

        private void applyAchievement(string updateSql,String reward)
        {
            Debug.WriteLine("applying Achievement "+reward);
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = updateSql;
                command.ExecuteNonQuery();
            }
            //New goods could be unlocked, populate market
            cities.PopulateCityMarketTable(cities.LoadCities(), cargoTypes.GetAllCargoTypesAndBasePrices());
        }

        public  string SubstitutePlaceholder(string originalString, string placeholder, string substitution)
        {
            // Check if the original string contains the placeholder
            if (originalString.Contains(placeholder))
            {
                // Replace the placeholder with the substitution and return the updated string
                return originalString.Replace(placeholder, substitution);
            }
            else
            {
                // If the placeholder doesn't exist, return the original string
                return originalString;
            }
        }
        public DataTable GetAchievements()
        {
            DataTable dataTable = new DataTable();


            using (var command = _connection.CreateCommand())
            {

                /*command.CommandText = @"
                    SELECT ID,
                       Name,
                       Text,
                       RewardText,
                       Target,
                       case
                            when achieved = 1 then '⚐'
                            else ''
                       end as achieved,
                       achievedDate
                  FROM achievements order by Key;
                ";*/
                //only show the achieved ones, and the first unachieved one per key
                command.CommandText = @"WITH FilteredAchievements AS (
    SELECT 
        ID,
        Name,
        Text,
        RewardText,
        Target,
        achieved,
        achievedDate,
        path,
        key,
        ROW_NUMBER() OVER (PARTITION BY path ORDER BY key ASC) as RowNum
    FROM achievements
    WHERE achieved = 0
),

AllAchievements AS (
    SELECT 
        ID,
        Name,
        Text,
        RewardText,
        Target,
        achieved,
        achievedDate,
        path,
        key
    FROM achievements
    WHERE achieved = 1

    UNION ALL

    SELECT 
        ID,
        Name,
        Text,
        RewardText,
        Target,
        achieved,
        achievedDate,
        path,
        key
    FROM FilteredAchievements
    WHERE RowNum = 1
)

SELECT
    --ID,
    Name,
    Text as Info,
    RewardText as Reward,
    Target,
    CASE
        WHEN achieved = 1 THEN '⚐'
        ELSE ''
    END as AchievedFlag,
    achievedDate as [Achieved On]
    --,
    --path,
    --key
FROM AllAchievements
ORDER BY path ASC, key ASC, achieved DESC;
";




                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                foreach (DataRow row in dataTable.Rows)
                {
                    row["Info"] = SubstitutePlaceholder((String)row["Info"], "{target}", "" + row["target"]);
                    
                }

            }

            return dataTable;
        }
    }
}
