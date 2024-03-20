using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data;

namespace Capital_and_Cargo
{
    internal class TransitManager
    {
        private SqliteConnection _connection;
        private GameDataManager dataManager;
        private Double kmPriceTruck = 0.01;
        private Double kmPricePlane = 0.06;
        public TransitManager(ref SqliteConnection connection, ref GameDataManager dataManager)
        {
            _connection = connection;
            EnsureTableExistsAndIsPopulated();
            this.dataManager = dataManager;
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
    CargoType TEXT NOT NULL,
    CargoAmount INTEGER NOT NULL, -- Assuming cargo amount is in units or kilograms, depending on cargo type
    TransportationMethod TEXT NOT NULL,
    Price REAL NOT NULL
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
            string sql = "SELECT OriginCity,DestinationCity,Progress,CargoType,CargoAmount FROM city_transit;";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText= sql;
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                
            }

            return dataTable;
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
    }
}
