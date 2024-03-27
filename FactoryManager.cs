using Microsoft.Data.Sqlite;
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
        //add comment

        public FactoryManager(ref SqliteConnection connection,String reputationCalculation)
        {
            _connection = connection;
            this.reputationCalculation = reputationCalculation;
            EnsureTableExistsAndIsPopulated();

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
                    AmountProduced INTEGER not null
                );";
                command.ExecuteNonQuery();
            }
        }
        public void buildFactory(String CityName, String CargoType)
        {
            (Boolean canBuild,String message) = canBuildFactory(CityName, CargoType);
            if(canBuild) {
                Debug.WriteLine("Build factory : TODO, NOT IMPLEMENTED");
            }
            else
            {
                Debug.WriteLine("Can't build factory : " + message);
            }

        }
        public (Boolean canBuild,String message) canBuildFactory(String CityName, String CargoType)
        {
            int cityReputation = 0;
            int requiredReputation = 0;
            Double Money;
            Int64 Reputation;
            (Money, Reputation) = getPlayerMoneyAndReputation(CityName);
            //TODO : check if the player has enough money and reputation
            String message = "";
            if (cityReputation >= requiredReputation)
            {
                return (true, message);
            }
            else
            {
                message = $@"You cannot build a factory yet.
                Your reputation in {CityName} is {cityReputation} and you need at least {requiredReputation}.
                You can get more reputation by importing, exporting, selling and buying goods in {CityName}.
                ";
                return (false, message);
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
        public DataTable LoadFactories(String city)
        {
            DataTable dataTable = new DataTable();
            string sql = @"SELECT 
                   CargoType as [Resource],
                   Level as [Factory Level],
                   AmountProduced as [Daily Production]
              FROM factories where CityName = @city;
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }

            }
            return dataTable;
        }
    }
}
