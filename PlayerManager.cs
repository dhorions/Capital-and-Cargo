using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Terminal.Gui;

namespace Capital_and_Cargo
{
    internal class PlayerManager
    {
        private SqliteConnection _connection;
        //add comment

        public PlayerManager(ref SqliteConnection connection)
        {
            _connection = connection;
            EnsureTableExistsAndIsPopulated();

        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("Player"))
            {
                CreatePlayerTable();
                InitPlayerTable();
            }
            if (!TableExists("warehouse"))
            {
                CreateWarehouseTable();
            }
            if (!TableExists("MoneyHistory"))
            {
                CreateMoneyHistoryTable();
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

        private void CreatePlayerTable()
        {
            string sql = @"
           CREATE TABLE Player (
    Date TEXT NOT NULL,
    Money REAL NOT NULL
);
";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        private void CreateMoneyHistoryTable()
        {
            string sql = @"
           CREATE TABLE MoneyHistory (
            Date TEXT NOT NULL,
            Money REAL NOT NULL
            );
            ";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        public void UpdateMoneyHistoryTable()
        {
            DataTable playerTable = LoadPlayer();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO MoneyHistory (Date, Money)
                    VALUES (@Date, @Money);";

                command.Parameters.AddWithValue("@Date", playerTable.Rows[0]["Date"]);
                command.Parameters.AddWithValue("@Money", playerTable.Rows[0]["Money"]);

                command.ExecuteNonQuery();
            }
        }
        public void InitPlayerTable()
        {

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Player (Date, Money)
                    VALUES (@Date, @Money);";

                command.Parameters.AddWithValue("@Date", "1900-01-01");
                command.Parameters.AddWithValue("@Money", 1000000000);

                command.ExecuteNonQuery();
            }


        }
        private void CreateWarehouseTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS warehouse (
                    CityName TEXT NOT NULL,
                    CargoType String NOT NULL,
                    Amount INTEGER NOT NULL
                );";
                command.ExecuteNonQuery();
            }
        }
        private void cleanupWarehouse()
        {
            //delete from warehouse where amount is 0
            //TODO : 
            using (var transaction = _connection.BeginTransaction())
            {
               
                //Remove goods with 0 amounts
                using (var command = _connection.CreateCommand())
                {

                    command.CommandText = @"delete from warehouse where amount <= 0";
                    int affected = command.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        Debug.WriteLine("Cleaning up the warehouse");
                    }
                }
                try
                {
                    //if there are multiple records for the same resource in the warehouse of a city, merge them
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"
                        -- Create a temporary table to store aggregated results
                        CREATE TEMPORARY TABLE warehouse_temp AS
                        SELECT CityName, CargoType, SUM(Amount) AS TotalAmount
                        FROM warehouse
                        GROUP BY CityName, CargoType;
                        -- Delete the original data from the `warehouse` table
                        DELETE FROM warehouse;
                        --Insert the aggregated data back into the `warehouse` table
                        INSERT INTO warehouse (CityName, CargoType, Amount)
                        SELECT CityName, CargoType, TotalAmount
                        FROM warehouse_temp;
                        --Drop the temporary table
                        DROP TABLE warehouse_temp;";
                        command.ExecuteNonQuery();


                    }
                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error making a purchase: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }



        }
        public DataTable loadWarehouse(String city)
        {
            cleanupWarehouse();

            DataTable dataTable = new DataTable();

            using (var command = _connection.CreateCommand())
            {

                command.CommandText = @"
            SELECT CargoType, Amount
            FROM warehouse 
            WHERE CityName = @CityName
            ORDER BY CargoType;";

                // Use parameters to prevent SQL injection
                command.Parameters.AddWithValue("@CityName", city);

                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }

            }

            return dataTable;
        }
        public void purchase(String city, String CargoType, int amount, Double price)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    //Decrease market supply
                    using (var command = _connection.CreateCommand())
                    {
                        Debug.WriteLine("Removing " + amount + " of " + CargoType + " from " + city + " market");
                        command.CommandText = @"
                               UPDATE city_market SET SupplyAmount = SupplyAmount - @amount WHERE CargoType = @cargoType and CityName = @city
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.ExecuteNonQuery();
                    }
                    //Pay
                    using (var command = _connection.CreateCommand())
                    {

                        Double totalPrice = amount * price;
                        Debug.WriteLine("Paying " + totalPrice);
                        command.CommandText = @"
                               UPDATE player SET money = money - @price 
                        ";
                        command.Parameters.AddWithValue("@price", totalPrice);
                        command.ExecuteNonQuery();
                    }
                    //Add to Warehouse
                    int recordsAffected = 0;
                    using (var command = _connection.CreateCommand())
                    {
                        Double totalPrice = amount * price;
                        Debug.WriteLine("Adding " + amount + " of " + CargoType + " to  " + city + " warehouse");
                        command.CommandText = @"
                               UPDATE warehouse SET Amount = Amount + @amount WHERE CityName = @city AND CargoType = @cargoType
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        recordsAffected = command.ExecuteNonQuery();
                    }
                    if (recordsAffected == 0)
                    {
                        //This cargo wasn't in the warehouse yet, add it
                        using (var cmdInsert = _connection.CreateCommand())
                        {
                            cmdInsert.CommandText = @"
                               INSERT INTO warehouse (CityName, CargoType, Amount) VALUES (@city, @cargoType, @amount)
                        ";
                            cmdInsert.Parameters.AddWithValue("@cargoType", CargoType);
                            cmdInsert.Parameters.AddWithValue("@city", city);
                            cmdInsert.Parameters.AddWithValue("@amount", amount);
                            cmdInsert.ExecuteNonQuery();
                        }

                    }


                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error making a purchase: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }
        }
        public void sell(String city, String CargoType, int amount, Double price)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    //Decrease market supply
                    using (var command = _connection.CreateCommand())
                    {
                        Debug.WriteLine("Adding " + amount + " of " + CargoType + " to " + city + " market");
                        command.CommandText = @"
                               UPDATE city_market SET SupplyAmount = SupplyAmount + @amount WHERE CargoType = @cargoType and CityName = @city
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.ExecuteNonQuery();
                    }
                    //Get Payed
                    using (var command = _connection.CreateCommand())
                    {

                        Double totalPrice = amount * price;
                        Debug.WriteLine("Getting Payed " + totalPrice);
                        command.CommandText = @"
                               UPDATE player SET money = money + @price 
                        ";
                        command.Parameters.AddWithValue("@price", totalPrice);
                        command.ExecuteNonQuery();
                    }
                    //Remove from to Warehouse
                    int recordsAffected = 0;
                    using (var command = _connection.CreateCommand())
                    {
                        Double totalPrice = amount * price;
                        Debug.WriteLine("Removing " + amount + " of " + CargoType + " from  " + city + " warehouse");
                        command.CommandText = @"
                               UPDATE warehouse SET Amount = Amount - @amount WHERE CityName = @city AND CargoType = @cargoType
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        recordsAffected = command.ExecuteNonQuery();
                    }



                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error making a sale: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }
        }
        public DataTable LoadPlayer()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT Date, Money from Player";

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
        public DataTable LoadPlayerHistory()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT Date, Money from MoneyHistory";

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
        public void nextDay()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Player
            SET Date = date(Date, '+1 day');";

                command.ExecuteNonQuery();
            }
            //

        }
    }
}
