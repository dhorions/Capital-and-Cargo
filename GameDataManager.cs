﻿using Microsoft.Data.Sqlite;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Diagnostics;

namespace Capital_and_Cargo
{
    internal class GameDataManager
    {
        
        public CitiesManager? cities;
        public TransitManager? transits;
        public CargoTypesManager? cargoTypes;
        public PlayerManager? player;
        public GameDataManager? dm;
        private SqliteConnection connection = new SqliteConnection("Data Source=CandC.db");
        public GameDataManager()
        {
           
            EnsureDatabaseCreated();
            initData();
        }

        private void initData()
        {
            this.dm = this;
            cities = new CitiesManager(ref this.connection,ref dm);
            transits = new TransitManager(ref this.connection,ref dm);
            cargoTypes = new CargoTypesManager(ref this.connection, ref dm);
            player = new PlayerManager(this.connection);
            cities.PopulateCityMarketTable(cities.LoadCities(), cargoTypes.GetAllCargoTypesAndBasePrices());
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
    }
}