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
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
            if(!TableExists("HistoryDetail"))
            {
                CreateHistoryDetailTable();
            }

        }

        private void CreateHistoryDetailTable()
        {
           

            var sql = @"CREATE TABLE HistoryDetail (
                Date      TEXT,
                Income    REAL default 0,
                Spend     REAL default 0,
                City      TEXT,
                CargoType TEXT,
                Import    INTEGER default 0,
                Export    INTEGER default 0,
                Production INTEGER default 0
            );
            CREATE UNIQUE INDEX idx_city_date_cargo ON HistoryDetail (City, Date, CargoType);
            CREATE INDEX idx_city_cargo ON HistoryDetail (City, CargoType);
            CREATE INDEX idx_city ON HistoryDetail (City);


            ";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
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
                Money REAL NOT NULL,
                productionBonusPool INTEGER default 0
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
        public DateTime firstOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }
        public void InitPlayerTable()
        {

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Player (Date, Money)
                    VALUES (@Date, @Money);";

                command.Parameters.AddWithValue("@Date", "1910-11-07");//first cargo flight was on nov 7, 1910
                command.Parameters.AddWithValue("@Money", 1000000);

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
                    Amount INTEGER NOT NULL,
                    PurchasePrice REAL NOT NULL
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
                        SELECT CityName, CargoType, SUM(Amount) AS TotalAmount, SUM(PurchasePrice) as PurchasePrice
                        FROM warehouse
                        GROUP BY CityName, CargoType;
                        -- Delete the original data from the `warehouse` table
                        DELETE FROM warehouse;
                        --Insert the aggregated data back into the `warehouse` table
                        INSERT INTO warehouse (CityName, CargoType, Amount,PurchasePrice)
                        SELECT CityName, CargoType, TotalAmount, PurchasePrice
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
            SELECT CargoType, Amount, (PurchasePrice / Amount) as [Cost], PurchasePrice as Value
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
        public DataTable getMaxSellAmount(string city, string cargoType)
        {
            DataTable maxSellAmount = new DataTable();
            string sql = "SELECT Amount FROM warehouse WHERE CityName = @city AND CargoType = @cargoType;";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@cargoType", cargoType);

                using (var reader = command.ExecuteReader())
                {
                    maxSellAmount.Load(reader);
                }

            }
            return maxSellAmount;
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
                               UPDATE city_market SET SupplyAmount = (SupplyAmount - @amount ) WHERE CargoType = @cargoType and CityName = @city
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.ExecuteNonQuery();
                    }
                    //Manage Reputation
                    using (var command = _connection.CreateCommand())
                    {


                        
                        command.CommandText = @"
                               UPDATE cities SET Bought = Bought + @amount where city = @city
                        ";
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@city", city);
                        command.ExecuteNonQuery();
                    }
                    //Pay
                    Double totalPrice = amount * price;
                    pay(totalPrice,city, CargoType);
                    //Add to Warehouse
                    int recordsAffected = 0;
                    using (var command = _connection.CreateCommand())
                    {
                        
                        Debug.WriteLine("Adding " + amount + " of " + CargoType + " to  " + city + " warehouse");
                        command.CommandText = @"
                               UPDATE warehouse SET Amount = Amount + @amount, PurchasePrice = PurchasePrice + @Price WHERE CityName = @city AND CargoType = @cargoType
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@Price", totalPrice);

                        recordsAffected = command.ExecuteNonQuery();
                    }
                    if (recordsAffected == 0)
                    {
                        //This cargo wasn't in the warehouse yet, add it
                        using (var cmdInsert = _connection.CreateCommand())
                        {
                            cmdInsert.CommandText = @"
                               INSERT INTO warehouse (CityName, CargoType, Amount, PurchasePrice) VALUES (@city, @cargoType, @amount,@Price)
                        ";
                            cmdInsert.Parameters.AddWithValue("@cargoType", CargoType);
                            cmdInsert.Parameters.AddWithValue("@city", city);
                            cmdInsert.Parameters.AddWithValue("@amount", amount);
                            cmdInsert.Parameters.AddWithValue("@Price", totalPrice);
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

        public void pay(double totalPrice,String city, String CargoType)
        {
            using (var command = _connection.CreateCommand())
            {


                Debug.WriteLine("Paying " + totalPrice);
                command.CommandText = @"
                               UPDATE player SET money = money - @price 
                        ";
                command.Parameters.AddWithValue("@price", totalPrice);
                command.ExecuteNonQuery();
            }
            //Keep track of money paid
            DateTime firstOfMonthDate = firstOfMonth(getCurrentDate());
            var sql = @"INSERT INTO HistoryDetail (City, Date, CargoType, Spend)
            VALUES (@city, @date, @CargoType, @Spend)
            ON CONFLICT (City, Date, CargoType) 
            DO UPDATE SET Spend = Spend + excluded.Spend;";
            using (var command = _connection.CreateCommand())
            {
                Debug.WriteLine("Storing income history " + firstOfMonthDate + "\t" + totalPrice + "\t" + city + "\t" + CargoType);
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@date", firstOfMonthDate);
                command.Parameters.AddWithValue("@CargoType", CargoType);
                command.Parameters.AddWithValue("@Spend", totalPrice);
                command.ExecuteNonQuery();
            }
        }

        public void sell(String city, String CargoType, Int64 amount, Double price)
        {
            /* using (var transaction = _connection.BeginTransaction())
             {
                 try
                 {*/
            //Increase market supply
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
                    Double totalPrice = amount * price;
                    receiveMoney(totalPrice, city, CargoType);
                   
                    //Manage Reputation
                    using (var command = _connection.CreateCommand())
                    {

                        command.CommandText = @"
                               UPDATE cities SET SOld = Sold + @amount where city = @city
                        ";
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@city", city);
                        command.ExecuteNonQuery();
                    }
                    //Remove from to Warehouse
                    int recordsAffected = 0;
                    using (var command = _connection.CreateCommand())
                    {
                        
                        Debug.WriteLine("Removing " + amount + " of " + CargoType + " from  " + city + " warehouse");
                        command.CommandText = @"
                               UPDATE warehouse SET Amount = Amount - @amount, PurchasePrice = PurchasePrice - @price WHERE CityName = @city AND CargoType = @cargoType
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", city);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@price", totalPrice);
                        recordsAffected = command.ExecuteNonQuery();
                    }



                    // Commit the transaction if both commands succeed
                 /*   transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error making a sale: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }

            }*/
        }
        private void receiveMoney(Double money, String city, String CargoType)
        {
            //Handle receiving money
            using (var command = _connection.CreateCommand())
            {
                Debug.WriteLine("Getting Payed " + money);
                command.CommandText = @"
                               UPDATE player SET money = money + @price 
                        ";
                command.Parameters.AddWithValue("@price", money);
                command.ExecuteNonQuery();
            }
            //Keep track of money received
            DateTime firstOfMonthDate = firstOfMonth(getCurrentDate());
            var sql = @"INSERT INTO HistoryDetail (City, Date, CargoType, Income)
            VALUES (@city, @date, @CargoType, @Income)
            ON CONFLICT (City, Date, CargoType) 
            DO UPDATE SET Income = Income + excluded.Income;";
            using (var command = _connection.CreateCommand())
            {
                Debug.WriteLine("Storing income history " + firstOfMonthDate + "\t" + money + "\t" + city + "\t" + CargoType);
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                command.Parameters.AddWithValue("@date", firstOfMonthDate);
                command.Parameters.AddWithValue("@CargoType", CargoType);
                command.Parameters.AddWithValue("@Income", money);
                command.ExecuteNonQuery();
            }


        }
        public DataTable LoadPlayer()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT Date, Money,productionBonusPool from Player";

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
        public DataTable LoadPlayerHistory(int limit)
        {
            DataTable dataTable = new DataTable();
            string sql = "select * from (SELECT Date, Money from MoneyHistory order by Date desc limit 0,@limit ) a order by date asc";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@limit", limit);
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
        public DateTime getCurrentDate() {
            DateTime currentDate = new DateTime();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "select Date from player";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var eventDate = reader["Date"];
                        currentDate = DateTime.ParseExact((String)reader["Date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                }
            }
            return currentDate;
        }
    }
}
