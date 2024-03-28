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
                createFactory(CityName, CargoType);
                //TODO: subtract money from player money
            }
            else
            {
                Debug.WriteLine("Can't build factory : " + message);
            }

        }
        public (Boolean canBuild,String message) canBuildFactory(String CityName, String CargoType)
        {
            //TODO : get city reputation for player and get required reputation to build the factory
            int cityReputation = 0;
            int requiredReputation = 0;
            Double Money;
            Int64 Reputation;
            (Money, Reputation) = getPlayerMoneyAndReputation(CityName);
            
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
        public void updateProduction()
        {
            Debug.WriteLine("Updating Production");
            //DataTable factories = LoadAllFactories();
            String sql = @"
                --Update records where there is already something in the inventory of the city for this cargotype
               UPDATE warehouse
                SET 
                    Amount = Amount + (
                        SELECT f.AmountProduced
                        FROM factories f
                        WHERE f.CityName = warehouse.CityName AND f.CargoType = warehouse.CargoType
                    ),
                    PurchasePrice = PurchasePrice + (
                        SELECT f.AmountProduced * cm.BuyPrice
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
                    f.AmountProduced, 
                    (f.AmountProduced * cm.BuyPrice) AS PurchasePrice
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

        }
    }
}
