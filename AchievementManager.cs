﻿using Microsoft.Data.Sqlite;
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
                São Paulo
                Tokyo
                Bogota
                Delhi
                Dubai
                Seoul
                Shanghai
                Guangzhou
                Beijing
                Shenzhen
                Mumbai
                Jakarta
                Singapore

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
                 Plastics
                 Electronics
                 Agricultural Products
                 Automobiles
                 Wood Products
                 Rubber
                 Footwear
                 Beverages
                 Furniture
                 Oil & Gas
                 Construction Materials
                 Textiles
                 Chemicals
                 Metals
                Glass Products
            Machinery
             Pharmaceuticals
            Tobacco Products
            * No Achievements Yet
               
                
                
               
                

            */

            //Reputation in a single city
            String sql_rep_any = "SELECT MIN(sum(" + reputationCalculation + "),{target}) as target FROM cities group by city order by target desc limit 1";
            InsertAchievement("rep/any/0000500", "rep/any", "Trade Pioneer", "Achieve {target} reputation in any city.", "Factory Building Unlocked", 500, sql_rep_any, "");
            InsertAchievement("rep/any/0005000", "rep/any", "Master Trader", "Achieve {target} reputation in any city.", "Production Bonus Points +10", 5000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}","10"));
            InsertAchievement("rep/any/0010000", "rep/any", "Civic Champion ", "Achieve {target} reputation in any city.", "Production Bonus Points +10", 10000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "10"));
            InsertAchievement("rep/any/0025000", "rep/any", "Local Kingpin", "Achieve {target} reputation in any city.", "Production Bonus Points +20", 25000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("rep/any/0050000", "rep/any", "Pillar of the Community", "Achieve {target} reputation in any city.", "Production Bonus Points +20", 50000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("rep/any/0100000", "rep/any", "Urban Influencer", "Achieve {target} reputation in any city.", "Production Bonus Points +20", 100000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("rep/any/0250000", "rep/any", "Key to the City", "Achieve {target} reputation in any city.", "Production Bonus Points +50", 250000, sql_rep_any, SubstitutePlaceholder(prodBonusSql, "{bonus}", "50"));
            InsertAchievement("rep/any/1000000", "rep/any", "City Magnate ", "Achieve {target} reputation in any city.", "Production Bonus Points +50", 1000000, sql_rep_any,  SubstitutePlaceholder(prodBonusSql, "{bonus}", "50"));

            //Total Reputation
            String sqlTotalRep = "SELECT MIN(sum(" + reputationCalculation + "),{target}) as target FROM cities desc limit 1";
            InsertAchievement("rep/total/0010000", "rep/total", "Global Luminary", "Achieve {target} reputation.", "Production Bonus Points +10", 10000, sqlTotalRep, SubstitutePlaceholder(prodBonusSql, "{bonus}", "10"));
            InsertAchievement("rep/total/0050000", "rep/total", "Worldwide Wunderkind", "Achieve {target} reputation.", "Unlock New City : Jakarta", 50000, sqlTotalRep, SubstitutePlaceholder(cityUnlockSql, "{city}", "Jakarta"));
            InsertAchievement("rep/total/0100000", "rep/total", "International Icon", "Achieve {target} reputation.", "Unlock new cargo Type : Toys", 100000, sqlTotalRep, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Toys"));
            InsertAchievement("rep/total/0250000", "rep/total", "Planetwide Patron", "Achieve {target} reputation.", "Unlock new cargo Type : Oil & Gas", 250000, sqlTotalRep, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Oil & Gas"));
            InsertAchievement("rep/total/0500000", "rep/total", "Universal Uplifter", "Achieve {target} reputation.", "Unlock New City : Singapore", 500000, sqlTotalRep, SubstitutePlaceholder(cityUnlockSql, "{city}", "Singapore"));
            InsertAchievement("rep/total/1000000", "rep/total", "Cosmopolitan Champion", "Achieve {target} reputation.", "Production Bonus Points +200", 1000000, sqlTotalRep, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("rep/total/2000000", "rep/total", "Earth's Emissary", "Achieve {target} reputation.", "Production Bonus Points +500", 2000000, sqlTotalRep, SubstitutePlaceholder(prodBonusSql, "{bonus}", "500"));
            InsertAchievement("rep/total/5000000", "rep/total", "Global Guardian", "Achieve {target} reputation.", "Production Bonus Points +5000", 5000000, sqlTotalRep, SubstitutePlaceholder(prodBonusSql, "{bonus}", "5000"));


            //Total Import per month
            String sql_import_total = "  SELECT MIN(sum(Import),{target}) as target FROM HistoryDetail group by Date order by target desc limit 1";
            InsertAchievement("imp/any/month/0000500", "imp/any/month", "New Importer on the Dock", "Import {target} goods in 1 month.", "Unlock new city : London", 500, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "London"));
            InsertAchievement("imp/any/month/0005000", "imp/any/month", "Container Rookie", "Import {target} goods in 1 month.", "Unlock new city : Los Angeles", 5000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Los Angeles"));
            InsertAchievement("imp/any/month/0010000", "imp/any/month", "Import Mogul", "Import {target} goods in 1 month.", "Unlock new city : Shanghai", 10000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Shanghai"));
            InsertAchievement("imp/any/month/0025000", "imp/any/month", "Freight Forwarder", "Import {target} goods in 1 month.", "Unlock new city : Houston", 25000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Houston"));
            InsertAchievement("imp/any/month/0050000", "imp/any/month", "Freight Forwarder", "Import {target} goods in 1 month.", "Unlock new city : Guangzhou", 50000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Guangzhou"));
            InsertAchievement("imp/any/month/0100000", "imp/any/month", "Import Mogul", "Import {target} goods in 1 month.", "Unlock new city : Mexico City", 100000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Mexico City"));
            InsertAchievement("imp/any/month/0250000", "imp/any/month", "Harbor Master", "Import {target} goods in 1 month.", "Unlock new city : São Paulo", 250000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "São Paulo"));
            InsertAchievement("imp/any/month/1000000", "imp/any/month", "Economic Engine", "Import {target} goods in 1 month.", "Unlock new city : Tokyo", 1000000, sql_import_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Tokyo"));



            //Total Export per month
            String sql_export_total = "  SELECT MIN(sum(Export),{target}) as target FROM HistoryDetail group by Date order by target desc limit 1";
            InsertAchievement("imp/any/month/0000500", "imp/any/month", "First-Time Exporter", "Export {target} goods in 1 month.", "Unlock new cargo type : Food products", 500, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{CargoType}", "Food products"));
            InsertAchievement("imp/any/month/0005000", "imp/any/month", "Export Enthusiast", "Export {target} goods in 1 month.", "Unlock new cargo type : Agricultural Products", 5000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{CargoType}", "Agricultural Products"));
            InsertAchievement("imp/any/month/0010000", "imp/any/month", "Export Enthusiast", "Export {target} goods in 1 month.", "Unlock new cargo type : Footwear", 10000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{CargoType}", "Footwear"));
            InsertAchievement("imp/any/month/0025000", "imp/any/month", "Captain of Commerce", "Export {target} goods in 1 month.", "Unlock new city : New York City", 25000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "New York City"));
            InsertAchievement("imp/any/month/0050000", "imp/any/month", "Continental Connector", "Export {target} goods in 1 month.", "Unlock new cargo type : Plastics", 50000, sql_export_total, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Plastics"));
            InsertAchievement("imp/any/month/0100000", "imp/any/month", "Continental Connector", "Export {target} goods in 1 month.", "Unlock new city : Buenos Aires", 100000, sql_export_total, SubstitutePlaceholder(cityUnlockSql, "{city}", "Buenos Aires"));
            InsertAchievement("imp/any/month/0250000", "imp/any/month", "Economic Expander", "Export {target} goods in 1 month.", "Unlock new cargo Type : Electronics", 250000, sql_export_total, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Electronics"));
            InsertAchievement("imp/any/month/1000000", "imp/any/month", "Economic Expander", "Export {target} goods in 1 month.", "Unlock new cargo Type : Automobiles", 1000000, sql_export_total, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Automobiles"));

            //Total Import in a city
            String sql_import_city = "  SELECT MIN(sum(Import),{target}) as target FROM HistoryDetail group by city order by target desc limit 1";
            InsertAchievement("imp/any/0000500", "imp/any", "Market Pioneer", "Import {target} goods into a city.", "Production Bonus Points +2", 500, sql_import_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "2"));
            InsertAchievement("imp/any/0005000", "imp/any", "Urban Supplier", "Import {target} goods into a city.", "Production Bonus Points +5", 5000, sql_import_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "5"));
            InsertAchievement("imp/any/0010000", "imp/any", "Town Trader", "Import {target} goods into a city.", "Production Bonus Points +10", 10000, sql_import_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "10"));
            InsertAchievement("imp/any/0025000", "imp/any", "City Stocker", "Import {target} goods into a city.", "Unlock new cargo Type : Food Products", 25000, sql_import_city, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Food Products"));
            InsertAchievement("imp/any/0050000", "imp/any", "Municipal Mogul", "Import {target} goods into a city.", "Production Bonus Points +20", 50000, sql_import_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "20"));
            InsertAchievement("imp/any/0100000", "imp/any", "Import Icon", "Import {target} goods into a city.", "Unlock New City : Bogota", 100000, sql_import_city, SubstitutePlaceholder(cityUnlockSql, "{city}", "Bogota"));
            InsertAchievement("imp/any/0250000", "imp/any", "Sovereign of Supply", "Import {target} goods into a city.", "Production Bonus Points +25", 250000, sql_import_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "25"));
            InsertAchievement("imp/any/1000000", "imp/any", "Capitalist Connector", "Import {target} goods into a city.", "Unlock New City : Delhi", 1000000, sql_import_city, SubstitutePlaceholder(cityUnlockSql, "{city}", "Delhi"));

            //Total Export from a city
            String sql_export_city = "  SELECT MIN(sum(Export),{target}) as target FROM HistoryDetail group by city order by target desc limit 1";
            InsertAchievement("exp/any/0000500", "exp/any", "Exporter Initiate", "Export {target} goods from a city.", "Production Bonus Points +2", 500, sql_export_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "2"));
            InsertAchievement("exp/any/0005000", "exp/any", "City Export Champion", "Export {target} goods from a city.", "Production Bonus Points +5", 5000, sql_export_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "5"));
            InsertAchievement("exp/any/0010000", "exp/any", "Metropolitan Merchant", "Export {target} goods from a city.", "Unlock new cargo Type : Wood Products", 10000, sql_export_city, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Wood Products"));
            InsertAchievement("exp/any/0025000", "exp/any", "Metropolitan Merchant", "Export {target} goods from a city.", "Unlock new cargo Type : Rubber", 25000, sql_export_city, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Rubber"));
            InsertAchievement("exp/any/0050000", "exp/any", "Global Gateway Guru", "Export {target} goods from a city.", "Unlock new cargo Type : Beverages", 50000, sql_export_city, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Beverages"));
            InsertAchievement("exp/any/0100000", "exp/any", "Global Gateway Guru", "Export {target} goods from a city.", "Production Bonus Points +35", 100000, sql_export_city, SubstitutePlaceholder(prodBonusSql, "{bonus}", "35"));
            InsertAchievement("exp/any/0250000", "exp/any", "Global Gateway Guru", "Export {target} goods from a city.", "Unlock New City : Dubai", 250000, sql_export_city, SubstitutePlaceholder(cityUnlockSql, "{city}", "Dubai"));
            InsertAchievement("exp/any/1000000", "exp/any", "Global Gateway Guru", "Export {target} goods from a city.", "Unlock New City : Seoul", 1000000, sql_export_city, SubstitutePlaceholder(cityUnlockSql, "{city}", "Seoul"));

            //Total nr of factories
            String sqlCountFactories = "select min(count(*),{target}) as target from factories";
            InsertAchievement("fact/count/00005", "fact/count", "Industrial Pioneer", "Build {target} factories.", "Production Bonus Points +10", 5, sqlCountFactories, SubstitutePlaceholder(prodBonusSql, "{bonus}", "10"));
            InsertAchievement("fact/count/00010", "fact/count", "Factory Founder", "Build {target} factories.", "Unlock new cargo Type : Furniture", 10, sqlCountFactories, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Furniture"));
            InsertAchievement("fact/count/00050", "fact/count", "Manufacturing Magnate", "Build {target} factories.", "Unlock New City : Shenzhen", 50, sqlCountFactories, SubstitutePlaceholder(cityUnlockSql, "{city}", "Shenzhen"));
            InsertAchievement("fact/count/00100", "fact/count", "Industrial Pioneer", "Build {target} factories.", "Production Bonus Points +100", 100, sqlCountFactories, SubstitutePlaceholder(prodBonusSql, "{bonus}", "100"));
            InsertAchievement("fact/count/00250", "fact/count", "Assembly Line Architect", "Build {target} factories.", "Unlock New City : Beijing", 250, sqlCountFactories, SubstitutePlaceholder(cityUnlockSql, "{city}", "Beijing"));
            InsertAchievement("fact/count/00500", "fact/count", "Production Powerhouse", "Build {target} factories.", "Unlock New City : Mumbai", 500, sqlCountFactories, SubstitutePlaceholder(cityUnlockSql, "{city}", "Mumbai"));

            //Different types of factories
            String sqlDistinctFactories = "SELECT min(count(distinct CargoType),{target}) as target from factories";
            InsertAchievement("fact/distinct/00002", "fact/distinct", "Diversified Developer", "Build {target} different factories.", "Unlock new cargo Type : Construction Materials", 2, sqlDistinctFactories, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Construction Materials"));
            InsertAchievement("fact/distinct/00005", "fact/distinct", "Industry Innovator", "Build {target} different factories.", "Unlock new cargo Type : Textiles", 5, sqlDistinctFactories, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Textiles"));
            InsertAchievement("fact/distinct/00010", "fact/distinct", "Sector Specialist", "Build {target} different factories.", "Unlock new cargo Type : Chemicals", 10, sqlDistinctFactories, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Chemicals"));
            InsertAchievement("fact/distinct/00015", "fact/distinct", "Factory Frontier", "Build {target} different factories.", "Unlock new cargo Type : Metals", 15, sqlDistinctFactories, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Metals"));
            InsertAchievement("fact/distinct/00020", "fact/distinct", "Manufacturing Medley", "Build {target} different factories.", "Production Bonus Points +500", 20, sqlDistinctFactories, SubstitutePlaceholder(prodBonusSql, "{bonus}", "500"));
            InsertAchievement("fact/distinct/00025", "fact/distinct", "Production Polyglot ", "Build {target} different factories.", "Production Bonus Points +5000", 25, sqlDistinctFactories, SubstitutePlaceholder(prodBonusSql, "{bonus}", "5000"));



            //Transports by truck
            String sqlTruckTransport = "select min(count (*),{target}) from HistoryDetail where cargoType like '%.truck'";
            InsertAchievement("transp/truck/00100", "transp/truck", "Rookie Roadster", "{target} transports by truck.", "Production Bonus Points +250", 100, sqlTruckTransport, SubstitutePlaceholder(prodBonusSql, "{bonus}", "250"));
            InsertAchievement("transp/truck/00500", "transp/truck", "Interstate Merchant", "{target} transports by truck.", "Unlock new cargo Type : Glass Products", 500, sqlTruckTransport, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Glass Products"));
            InsertAchievement("transp/truck/02000", "transp/truck", "Heavy Hauler", "{target} transports by truck.", "Unlock new cargo Type : Machinery", 2000, sqlTruckTransport, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", "Machinery"));
            InsertAchievement("transp/truck/05000", "transp/truck", "Coast-to-Coast Conductor", "{target} transports by truck.", "Unlock new cargo Type :  Pharmaceuticals", 5000, sqlTruckTransport, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", " Pharmaceuticals"));
            InsertAchievement("transp/truck/05000", "transp/truck", "Trucking Tycoon", "{target} transports by truck.", "Unlock new cargo Type :  Tobacco Products", 10000, sqlTruckTransport, SubstitutePlaceholder(cargoUnlockSql, "{CargoType}", " Tobacco Products"));

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
                    String formattedTarget = String.Format("{0:N0}", row["target"]);
                    row["Info"] = SubstitutePlaceholder((String)row["Info"], "{target}", "" + formattedTarget);
                    
                }

            }

            return dataTable;
        }
    }
}
