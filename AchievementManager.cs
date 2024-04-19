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

                Double result = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
                        }
                    }
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
