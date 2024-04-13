using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Capital_and_Cargo
{
    internal class TransitManager
    {
        private SqliteConnection _connection;
        private GameDataManager dataManager;
        private PlayerManager player;
        private Double kmPriceTruck = 0.0002;
        private Double kmPricePlane = 0.001;
        private Double speedTruck = 100;
        private Double speedPlane = 500;
        public TransitManager(ref SqliteConnection connection, ref GameDataManager dataManager, ref PlayerManager player)
        {
            _connection = connection;
            EnsureTableExistsAndIsPopulated();
            this.dataManager = dataManager;
            this.player = player;
        }

        public void EnsureTableExistsAndIsPopulated()
        {
            if (!TableExists("city_transit"))
            {
                CreateTransitTable();
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

        private void CreateTransitTable()
        {
            string sql = @"
            CREATE TABLE city_transit (
    TransitID INTEGER PRIMARY KEY AUTOINCREMENT,
    OriginCity TEXT NOT NULL,
    DestinationCity  TEXT NOT NULL,
    Distance REAL NOT NULL, -- Assuming distance is in kilometers
    Progress REAL NOT NULL, -- Assuming progress is a percentage (0-100)
    ProgressKM REAL NOT NULL,
    CargoType TEXT NOT NULL,
    CargoAmount INTEGER NOT NULL, -- Assuming cargo amount is in units or kilograms, depending on cargo type
    TransportationMethod TEXT NOT NULL,
    Price REAL NOT NULL,
PurchasePrice REAL NOT NULL
);
";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

       
        public DataTable LoadTransit()
        {
            DataTable dataTable = new DataTable();
            string sql = "SELECT OriginCity as Origin,DestinationCity as Destination,CargoType as Cargo,CargoAmount  as Amount,Progress as progressPercentage,TransportationMethod FROM city_transit;";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText= sql;
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                
            }
            var col = dataTable.Columns.Add("Progress");
            col.SetOrdinal(0);
            
            foreach (DataRow row in dataTable.Rows)
            {

                //Convert the progress into a progress indicator
                Double progress = (Double)row["progressPercentage"];
                row["Progress"] = createProgressIndicatorString(progress, (String)row["TransportationMethod"]);
                
                    
            }
            dataTable.Columns.Remove("progressPercentage");
            dataTable.Columns.Remove("TransportationMethod");
            return dataTable;
        }
        private String createProgressIndicatorString(Double progress,String transportMethod)
        {

            // Unicode character for a plane

            string plane = "\u2708"; // Alternatively, use char.ConvertFromUtf32(0x2708)

            // Unicode character for a truck
            string truck = "\u26CD"; // Alternatively, use char.ConvertFromUtf32(0x1F69A)
            String indicator = ">";
            if(transportMethod == "plane")
            {
                indicator = plane;
            }
            if (transportMethod == "truck")
            {
                indicator = truck;
            }

            // Define the total length of the scale (number of dashes + arrow)
            int scaleLength = 10;

            // Calculate the position of the arrow based on the percentage
            // Since the scale is 10 characters long, divide by 10 to find the position
            int arrowPosition = (int)Math.Round(progress / 10.0);

            // Create the string representation
            string result = string.Empty;
            for (int i = 1; i <= scaleLength; i++)
            {
                if (i == arrowPosition)
                {
                    result += indicator;
                }
                else
                {
                    result += " ";
                }
            }

            return result;
        }
        private (double lat,double lon) getCityCoordinates(String city)
        {
            string sql = "SELECT Latitude, Longitude from cities where city = @city";
            double lat = 0;
            double lon = 0;
            DataTable dataTable = new DataTable();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@city", city);
                using (var reader = command.ExecuteReader())
                {
                  
                    dataTable.Load(reader);
                    lat = (Double)dataTable.Rows[0]["Latitude"];
                    lon = (Double)dataTable.Rows[0]["Longitude"];
                }

            }
            return (lat, lon);
        }
        public Boolean canBeTransported(String transportType, String originCity, String targetCity)
        {
            if (transportType == "plane") return true;
            DataTable dataTable = new DataTable();
            Debug.WriteLine("Checking if a truck transport between " + originCity +" and " + targetCity +" is possible");
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"SELECT 
                    c1.city AS City1, 
                    c2.city AS City2, 
                    CASE 
                        WHEN c1.continent = c2.continent THEN 'Yes' 
                        ELSE 'No' 
                    END AS SameContinent
                FROM 
                    cities c1, 
                    cities c2
                WHERE 
                    c1.city = @originCity AND 
                    c2.city = @TargetCity;";
                command.Parameters.AddWithValue("@originCity", originCity);
                command.Parameters.AddWithValue("@TargetCity", targetCity);
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                if ((String)dataTable.Rows[0]["SameContinent"] == "Yes") return true;
            }
            return false;
        }
        public (Double distance,Double price) getTransportPrice(String transportType,String originCity, String targetCity)
        {
            var (lat1, lon1) = getCityCoordinates(originCity);
            var (lat2, lon2) = getCityCoordinates(targetCity);
            Double distance = CalculateDistance(lat1, lon1,lat2,lon2) ;
            Double price = 0.0;
            if(transportType == "plane")
            {
                price = distance * kmPricePlane;
            }
            else
            {
                price = distance * kmPriceTruck;
            }
            return (distance, price);
        }
        private  double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in kilometers
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c; // Distance in kilometers
            return distance;
        }

        private  double DegreesToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
        public void updateTransits()
        {
            

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    var sql = @"
                            UPDATE city_transit
                            SET ProgressKM = ProgressKM + @speed,
                            Progress = ((ProgressKM + @speed) / Distance) * 100
                            where TransportationMethod = @TransportationMethod and Progress < 100
                                        ";
                    //Debug.WriteLine("Updating transport progress");
                    using (var command = _connection.CreateCommand())
                    {
                        
                        command.CommandText = sql;
                        
                        command.Parameters.AddWithValue("@speed", speedTruck);
                        command.Parameters.AddWithValue("@TransportationMethod", "truck");
                       int affected =  command.ExecuteNonQuery();
                        if(affected > 0)
                        {
                            Debug.WriteLine("moving trucks \t" + affected);
                        }
                    }
                    using (var command = _connection.CreateCommand())
                    {
                       
                        command.CommandText = sql;
                        command.Parameters.AddWithValue("@speed", speedPlane);
                        command.Parameters.AddWithValue("@TransportationMethod", "plane");
                        int affected = command.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            Debug.WriteLine("moving planes \t" + affected);
                        }
                    }
                        
                    
                    //If something arrives, add it to the warehouse
                    DataTable dataTable = new DataTable();
                    
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = " select * from city_transit where progress >= 100";
                        using (var reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                    if(dataTable.Rows.Count > 0)
                    {
                        Debug.WriteLine("Offloading " + dataTable.Rows.Count + " loads of Cargo "  );
                    }
                    //Add all these to the Warehouse
                    foreach (DataRow row in dataTable.Rows)
                    {

                        
                        using (var command = _connection.CreateCommand())
                        {
                            //Debug.WriteLine("Adding to warehouse of " + row["DestinationCity"] + " \t" + row["CargoAmount"] + " " + row["CargoType"]);
                            command.CommandText = @"
                            INSERT INTO warehouse (
                                  CityName,
                                  CargoType,
                                  Amount,
                                  PurchasePrice
                              )
                              VALUES (
                                  @city,
                                  @CargoType,
                                  @Amount,
                                  @PurchasePrice
                              );

                                        ";
                            command.Parameters.AddWithValue("@city", row["DestinationCity"]);
                            command.Parameters.AddWithValue("@CargoType", row["CargoType"]);
                            command.Parameters.AddWithValue("@Amount", row["CargoAmount"]);
                            command.Parameters.AddWithValue("@PurchasePrice", row["PurchasePrice"]);
                            Debug.WriteLine("\t " + row["DestinationCity"] + "\t" + row["CargoAmount"] + "\t" + row["CargoAmount"]);
                            command.ExecuteNonQuery();
                            //Manage Reputation
                            
                        }
                        using (var command = _connection.CreateCommand())
                        {

                            command.CommandText = @"
                               UPDATE cities SET Imported = Imported + @amount where city = @city
                        ";
                            command.Parameters.AddWithValue("@amount", row["CargoAmount"]);
                            command.Parameters.AddWithValue("@city", row["DestinationCity"]);
                            command.ExecuteNonQuery();
                        }
                        //Keep track of cargo arrived received
                        DateTime firstOfMonthDate = player.firstOfMonth(player.getCurrentDate());
                        var sqlH = @"INSERT INTO HistoryDetail (City, Date, CargoType, Import)
                        VALUES (@city, @date, @CargoType, @Import)
                        ON CONFLICT (City, Date, CargoType) 
                        DO UPDATE SET Import = Import + excluded.Import;";
                        using (var command = _connection.CreateCommand())
                        {
                            Debug.WriteLine("Storing import history " + firstOfMonthDate + "\t" + row["CargoAmount"] + "\t" + row["DestinationCity"] + "\t" + row["CargoType"]);
                            command.CommandText = sqlH;
                            command.Parameters.AddWithValue("@city", row["DestinationCity"]);
                            command.Parameters.AddWithValue("@date", firstOfMonthDate);
                            command.Parameters.AddWithValue("@CargoType", row["CargoType"]);
                            command.Parameters.AddWithValue("@Import", row["CargoAmount"]);
                            command.ExecuteNonQuery();
                        }



                    }
                    using (var cmdDelete = _connection.CreateCommand())
                    {
                        //Debug.WriteLine("Deleting all finished transports");
                        cmdDelete.CommandText = @"delete from city_transit where progress >= 100;";

                        cmdDelete.ExecuteNonQuery();
                    }





                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error updating transports {ex.Message} {ex.Source}");
                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }
        }
        public void transport(String transportationMode, String originCity, String targetCity, String CargoType, int amount)
        {
            var (distance, price) = getTransportPrice(transportationMode, originCity, targetCity);

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    Double cargoValue = 0;
                    //Get current price from Warehouse
                    using (var command = _connection.CreateCommand())
                    {
                        
                        command.CommandText = @"
                               select PurchasePrice / Amount as PurchaseUnitPrice from warehouse WHERE CargoType = @cargoType and CityName = @city
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", originCity);
                        using (var reader = command.ExecuteReader())
                        {
                            DataTable ppTable = new DataTable();
                            ppTable.Load(reader);
                            cargoValue = (Double)ppTable.Rows[0]["PurchaseUnitPrice"] * amount;
                        }

                    }
                    //Decrease warehouse supply
                    using (var command = _connection.CreateCommand())
                    {
                        Debug.WriteLine("Removing " + amount + " of " + CargoType + " from " + originCity + " warehouse");
                        command.CommandText = @"
                               UPDATE warehouse SET Amount = Amount - @amount, PurchasePrice = PurchasePrice - (PurchasePrice / Amount)  WHERE CargoType = @cargoType and CityName = @city
                        ";
                        command.Parameters.AddWithValue("@cargoType", CargoType);
                        command.Parameters.AddWithValue("@city", originCity);
                        command.Parameters.AddWithValue("@amount", amount);

                        command.ExecuteNonQuery();
                    }
                    //Manage Reputation
                    using (var command = _connection.CreateCommand())
                    {

                        command.CommandText = @"
                               UPDATE cities SET Exported = Exported + @amount where city = @city
                        ";
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@city", originCity);
                        command.ExecuteNonQuery();
                    }
                    //Pay
                    /*using (var command = _connection.CreateCommand())
                    {

                        
                        Debug.WriteLine("Paying " + price);
                        command.CommandText = @"
                               UPDATE player SET money = money - @price 
                        ";
                        command.Parameters.AddWithValue("@price", price * amount);
                        command.ExecuteNonQuery();
                    }*/
                    player.pay(price * amount, originCity, CargoType + ".transport." + transportationMode);
                    
                   
                        
                        using (var cmdInsert = _connection.CreateCommand())
                        {
                            cmdInsert.CommandText = @"
                            INSERT INTO city_transit (
                             OriginCity,
                             DestinationCity,
                             Distance,
                             Progress,
                             ProgressKm,
                             CargoType,
                             CargoAmount,
                             TransportationMethod,
                             Price,
                             PurchasePrice
                         )
                         VALUES (
                            
                            @OriginCity,
                             @DestinationCity,
                             @Distance,
                             @Progress,
                            @ProgressKm,
                             @CargoType,
                             @CargoAmount,
                             @TransportationMethod,
                             @Price,
                            @PurchasePrice
                         );
                        ";
                        cmdInsert.Parameters.AddWithValue("@CargoType", CargoType);
                        cmdInsert.Parameters.AddWithValue("@OriginCity", originCity);
                        cmdInsert.Parameters.AddWithValue("@DestinationCity", targetCity);
                        cmdInsert.Parameters.AddWithValue("@CargoAmount", amount);
                        cmdInsert.Parameters.AddWithValue("@Distance", distance);
                        cmdInsert.Parameters.AddWithValue("@TransportationMethod", transportationMode);
                        cmdInsert.Parameters.AddWithValue("@Progress", 0);
                        cmdInsert.Parameters.AddWithValue("@ProgressKm", 0);
                        cmdInsert.Parameters.AddWithValue("@Price", price);
                        cmdInsert.Parameters.AddWithValue("@PurchasePrice", cargoValue);
                        cmdInsert.ExecuteNonQuery();
                        }
                        //Keep track of cargo export
                        DateTime firstOfMonthDate = player.firstOfMonth(player.getCurrentDate());
                        var sqlH = @"INSERT INTO HistoryDetail (City, Date, CargoType, Export)
                            VALUES (@city, @date, @CargoType, @Export)
                            ON CONFLICT (City, Date, CargoType) 
                            DO UPDATE SET Export = Export + excluded.Export;";
                        using (var command = _connection.CreateCommand())
                        {
                            Debug.WriteLine("Storing export history " + firstOfMonthDate + "\t" + amount + "\t" + originCity + "\t" + CargoType);
                            command.CommandText = sqlH;
                            command.Parameters.AddWithValue("@city", originCity);
                            command.Parameters.AddWithValue("@date", firstOfMonthDate);
                            command.Parameters.AddWithValue("@CargoType", CargoType);
                            command.Parameters.AddWithValue("@Export", amount);
                            command.ExecuteNonQuery();
                        }




                    // Commit the transaction if both commands succeed
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error registering a transport: {ex.Message}");

                    // Rollback the transaction on error
                    transaction.Rollback();
                }
            }
        }
    }

}
