﻿using Microsoft.Data.Sqlite;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using System.Globalization;
using System.Runtime.Intrinsics.X86;

namespace Capital_and_Cargo
{
    internal class GameDataManager
    {
        
        public CitiesManager? cities;
        public TransitManager? transits;
        public CargoTypesManager? cargoTypes;
        public PlayerManager? player;
        public GameDataManager? dm;
        public FactoryManager ? factory;
        public SoundMananger? SoundMananger;
        public AchievementManager? achievements;
        private SqliteConnection connection;
        private String reputationCalculation = "";
        private static Double importReputation = .50;
        private static Double exportReputation = .25;
        private static Double sellReputation = .15;
        private static Double buyReputation = .10;
        public GameDataManager()
        {
            initDb();
            EnsureDatabaseCreated();
            initData();
        }

        private void initDb()
        {
           
            string dbPath = Path.Combine(getDataPath(), "CandC.db"); 
            Debug.WriteLine("Database Path:" + dbPath);
            connection =  new SqliteConnection($"Data Source={dbPath}");
        }
        private String getDataPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = Path.Combine(appDataPath, "Capital and Cargo");
            Directory.CreateDirectory(appDataPath);
            return appDataPath;
        }

        private void initData()
        {
            reputationCalculation = $"(Bought * {buyReputation.ToString(CultureInfo.InvariantCulture)}) + (Sold * {sellReputation.ToString(CultureInfo.InvariantCulture)}) + (Imported * {importReputation.ToString(CultureInfo.InvariantCulture)}) + (Exported * {exportReputation.ToString(CultureInfo.InvariantCulture)})";
            this.dm = this;
            SoundMananger = new SoundMananger();
            player = new PlayerManager(ref this.connection);
            cargoTypes = new CargoTypesManager(ref this.connection, ref dm);
            factory = new FactoryManager(ref this.connection, reputationCalculation, ref cargoTypes, ref player,  ref SoundMananger);
            cities = new CitiesManager(ref this.connection, ref dm, reputationCalculation, ref player, ref factory);
            cities.PopulateCityMarketTable(cities.LoadCities(), cargoTypes.GetAllCargoTypesAndBasePrices());
            factory.setCitiesManager(cities);
            achievements = new AchievementManager(ref this.connection, reputationCalculation,ref cities,ref cargoTypes);
            transits = new TransitManager(ref this.connection,ref dm,ref player,ref cities,ref factory);
            factory.setTransitManager(transits);




        }

        public void EnsureDatabaseCreated()
        {
            Debug.WriteLine("opening db");
            // Open the connection
            this.connection.Open();
            

            // Optionally, execute queries to set up the database schema.
            // For example, creating tables if they don't exist.
             using (var command = this.connection.CreateCommand())
             {
                 command.CommandText = @"CREATE TABLE IF NOT EXISTS MyTable (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT);";
                 command.ExecuteNonQuery();
             }
                        // Close the connection
            //this.connection.Close();
        }

        // Make sure to properly dispose of the connection when the DatabaseHelper instance is being disposed
        public void Dispose()
        {
            if (this.connection != null)
            {
                this.connection.Close();
                this.connection.Dispose();
                this.connection = null;
            }
        }
        private bool IsFirstDayOfMonth(DateTime date)
        {
            return date.Day == 1;
            
        }
        private bool IsFirstDayOfWeek(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Monday;

        }
        
        public void gameUpdateLoop()
        {
            
            player.nextDay();
            //Task.Run(() => processDay());
            processDay();
        }
        private void processDay()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            System.Data.DataTable p = player.LoadPlayer();
            DateTime currentDay = DateTime.ParseExact((String)p.Rows[0]["Date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (IsFirstDayOfMonth(currentDay))
            {
                Debug.WriteLine("First Day of new Month : " + currentDay);
                //Update city market prices
                cities.UpdateCityMarketTable(cities.LoadCities(), currentDay);
                //Capture historical data
                player.UpdateMoneyHistoryTable();
            }
            if (IsFirstDayOfWeek(currentDay))
            {
                factory.updateProduction();
            }
            transits.updateTransits();
            achievements.checkAchievements(currentDay);
            stopwatch.Stop();
            Debug.WriteLine($"-> {currentDay} -  {stopwatch.ElapsedMilliseconds} ms");
        }
        public void purchase(String city, String CargoType, int amount, Double price)
        {
            player.purchase(city,CargoType,amount,price);
        }
    }
}
