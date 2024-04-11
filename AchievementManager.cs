using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Capital_and_Cargo
{
    internal class AchievementManager
    {
        private SqliteConnection _connection;
        private String reputationCalculation;
        public AchievementManager(ref SqliteConnection connection, String reputationCalculation)
        {
            _connection = connection;
            this.reputationCalculation = reputationCalculation;
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
            String sql_rep_any = "SELECT MIN(sum(" + reputationCalculation + "),{target}) as target FROM cities group by city order by target desc limit 1";
            InsertAchievement("rep/any/0000500", "rep/any", "Trade Pioneer", "Achieve {target} reputation in any city.", "Factory Building Unlocked", 500, sql_rep_any, "");
            InsertAchievement("rep/any/0005000", "rep/any", "Master Trader", "Achieve {target} reputation in any city.", "", 5000, sql_rep_any, "");
            InsertAchievement("rep/any/0250000", "rep/any", "Local Kingpin", "Achieve {target} reputation in any city.", "", 250000, sql_rep_any, "");
            InsertAchievement("rep/any/1000000", "rep/any", "City Magnate ", "Achieve {target} reputation in any city.", "", 1000000, sql_rep_any, "");


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
                        }
                    }
                }



            }


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
               
                command.CommandText = @"
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
                ";

               

                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
                foreach (DataRow row in dataTable.Rows)
                {
                    row["Text"] = SubstitutePlaceholder((String)row["Text"], "{target}", "" + row["target"]);
                    
                }

            }

            return dataTable;
        }
    }
}
