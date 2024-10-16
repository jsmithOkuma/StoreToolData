using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace StoreToolData
{
    class SQLiteOperations : IDatabaseOperations
    {
        private string connectionString;

        Log log;

        public SQLiteOperations(string connStr)
        {
            this.connectionString = connStr;
            log = new Log();

            log.AddLogMessage("Initialize SQLite operations: " + connStr);
        }

        private SQLiteConnection CreateConnection()
        {
            return new SQLiteConnection(connectionString);
        }


        public void CheckAndCreateTable()
        {
            try
            {
                if (!File.Exists("tools.sqlite"))
                {
                    SQLiteConnection.CreateFile("tools.sqlite");
                    Console.WriteLine("Database file tools.sqlite created.");
                }

                using (SQLiteConnection connection = CreateConnection())
                {
                    connection.Open();

                    string checkTableExistsQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Tools';";
                    using (SQLiteCommand cmd = new SQLiteCommand(checkTableExistsQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            string createTableQuery = @"
                    CREATE TABLE Tools (
                        ToolNumber INTEGER,
                        PotNum INTEGER,
                        ToolLife INTEGER,
                        ToolLifeRem INTEGER,
                        ToolName TEXT,
                        ToolLengthOffset REAL,
                        ToolIsRegistered TEXT
                    )";

                            using (SQLiteCommand createCmd = new SQLiteCommand(createTableQuery, connection))
                            {
                                createCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    connection.Close();
                }
            } catch(Exception ex)
            {
                log.AddLogMessage("Problem checking and creating table for SQL Server: " + connectionString + "\n" + ex.ToString());
            }
        }

        public void InsertTools(List<Tool> tools)
        {
            try
            {
                using (SQLiteConnection connection = CreateConnection())
                {
                    connection.Open();
                    List<string> columns = GetTableColumns("Tools");

                    List<int> dbToolNumbers = new List<int>();
                    using (SQLiteCommand cmdFetch = new SQLiteCommand("SELECT ToolNumber FROM Tools", connection))
                    {
                        using (SQLiteDataReader reader = cmdFetch.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dbToolNumbers.Add(reader.GetInt32(0));
                            }
                        }
                    }

                    foreach (Tool tool in tools)
                    {
                        string checkExistQuery = "SELECT * FROM Tools WHERE ToolNumber = @ToolNumber";
                        bool toolExists = false;

                        using (SQLiteCommand cmdCheck = new SQLiteCommand(checkExistQuery, connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@ToolNumber", tool.ToolNumber);
                            using (SQLiteDataReader reader = cmdCheck.ExecuteReader())
                            {
                                toolExists = reader.HasRows;
                                reader.Close();
                            }
                        }

                        dbToolNumbers.Remove(tool.ToolNumber);

                        if (toolExists)
                        {
                            StringBuilder updateBuilder = new StringBuilder("UPDATE Tools SET ");
                            if (columns.Contains("PotNum")) updateBuilder.Append("PotNum = @PotNum, ");
                            if (columns.Contains("ToolLife")) updateBuilder.Append("ToolLife = @ToolLife, ");
                            if (columns.Contains("ToolLifeRem")) updateBuilder.Append("ToolLifeRem = @ToolLifeRem, ");
                            if (columns.Contains("ToolName")) updateBuilder.Append("ToolName = @ToolName, ");
                            if (columns.Contains("ToolLengthOffset")) updateBuilder.Append("ToolLengthOffset = @ToolLengthOffset, ");
                            if (columns.Contains("ToolIsRegistered")) updateBuilder.Append("ToolIsRegistered = @ToolIsRegistered ");

                            string updateQuery = updateBuilder.ToString().Trim().TrimEnd(',') + " WHERE ToolNumber = @ToolNumber";

                            using (SQLiteCommand cmdUpdate = new SQLiteCommand(updateQuery, connection))
                            {
                                cmdUpdate.Parameters.AddWithValue("@ToolNumber", tool.ToolNumber);
                                if (columns.Contains("PotNum")) cmdUpdate.Parameters.AddWithValue("@PotNum", tool.PotNum);
                                if (columns.Contains("ToolLife")) cmdUpdate.Parameters.AddWithValue("@ToolLife", tool.ToolLife);
                                if (columns.Contains("ToolLifeRem")) cmdUpdate.Parameters.AddWithValue("@ToolLifeRem", tool.ToolLifeRem);
                                if (columns.Contains("ToolName")) cmdUpdate.Parameters.AddWithValue("@ToolName", tool.ToolName);
                                if (columns.Contains("ToolLengthOffset")) cmdUpdate.Parameters.AddWithValue("@ToolLengthOffset", tool.ToolLengthOffset);
                                if (columns.Contains("ToolIsRegistered")) cmdUpdate.Parameters.AddWithValue("@ToolIsRegistered", tool.ToolIsAttached);
                                cmdUpdate.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            StringBuilder insertBuilder = new StringBuilder("INSERT INTO Tools (");
                            if (columns.Contains("ToolNumber")) insertBuilder.Append("ToolNumber, ");
                            if (columns.Contains("PotNum")) insertBuilder.Append("PotNum, ");
                            if (columns.Contains("ToolLife")) insertBuilder.Append("ToolLife, ");
                            if (columns.Contains("ToolLifeRem")) insertBuilder.Append("ToolLifeRem, ");
                            if (columns.Contains("ToolName")) insertBuilder.Append("ToolName, ");
                            if (columns.Contains("ToolLengthOffset")) insertBuilder.Append("ToolLengthOffset, ");
                            if (columns.Contains("ToolIsRegistered")) insertBuilder.Append("ToolIsRegistered ");

                            string insertQuery = insertBuilder.ToString().Trim().TrimEnd(',') + ") VALUES (";
                            if (columns.Contains("ToolNumber")) insertQuery += "@ToolNumber, ";
                            if (columns.Contains("PotNum")) insertQuery += "@PotNum, ";
                            if (columns.Contains("ToolLife")) insertQuery += "@ToolLife, ";
                            if (columns.Contains("ToolLifeRem")) insertQuery += "@ToolLifeRem, ";
                            if (columns.Contains("ToolName")) insertQuery += "@ToolName, ";
                            if (columns.Contains("ToolLengthOffset")) insertQuery += "@ToolLengthOffset, ";
                            if (columns.Contains("ToolIsRegistered")) insertQuery += "@ToolIsRegistered ";
                            insertQuery = insertQuery.Trim().TrimEnd(',') + ")";

                            using (SQLiteCommand cmdInsert = new SQLiteCommand(insertQuery, connection))
                            {
                                if (columns.Contains("ToolNumber")) cmdInsert.Parameters.AddWithValue("@ToolNumber", tool.ToolNumber);
                                if (columns.Contains("PotNum")) cmdInsert.Parameters.AddWithValue("@PotNum", tool.PotNum);
                                if (columns.Contains("ToolLife")) cmdInsert.Parameters.AddWithValue("@ToolLife", tool.ToolLife);
                                if (columns.Contains("ToolLifeRem")) cmdInsert.Parameters.AddWithValue("@ToolLifeRem", tool.ToolLifeRem);
                                if (columns.Contains("ToolName")) cmdInsert.Parameters.AddWithValue("@ToolName", tool.ToolName);
                                if (columns.Contains("ToolLengthOffset")) cmdInsert.Parameters.AddWithValue("@ToolLengthOffset", tool.ToolLengthOffset);
                                if (columns.Contains("ToolIsRegistered")) cmdInsert.Parameters.AddWithValue("@ToolIsRegistered", tool.ToolIsAttached);
                                cmdInsert.ExecuteNonQuery();
                            }
                        }
                    }

                    foreach (int oldToolNumber in dbToolNumbers)
                    {
                        using (SQLiteCommand cmdDelete = new SQLiteCommand("DELETE FROM Tools WHERE ToolNumber = @ToolNumber", connection))
                        {
                            cmdDelete.Parameters.AddWithValue("@ToolNumber", oldToolNumber);
                            cmdDelete.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLogMessage("Problem inserting tools to SQL Server: " + connectionString + "\n" + ex.ToString());
            }
        }

        public List<Tool> GetTools()
        {
            List<Tool> tools = new List<Tool>();
            try
            {
                using (SQLiteConnection conn = CreateConnection())
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = "SELECT * FROM Tools";
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Tool tool = new Tool();

                                if (reader.GetOrdinal("ToolNumber") >= 0)
                                    tool.ToolNumber = Convert.ToInt32(reader["ToolNumber"]);
                                if (reader.GetOrdinal("PotNum") >= 0)
                                    tool.PotNum = Convert.ToInt32(reader["PotNum"]);
                                if (reader.GetOrdinal("ToolLife") >= 0)
                                    tool.ToolLife = Convert.ToInt32(reader["ToolLife"]);
                                if (reader.GetOrdinal("ToolLifeRem") >= 0)
                                    tool.ToolLifeRem = Convert.ToInt32(reader["ToolLifeRem"]);
                                if (reader.GetOrdinal("ToolName") >= 0)
                                    tool.ToolName = Convert.ToString(reader["ToolName"]);
                                if (reader.GetOrdinal("ToolLengthOffset") >= 0)
                                    tool.ToolLengthOffset = Convert.ToInt32(reader["ToolLengthOffset"]);
                                if (reader.GetOrdinal("ToolIsRegistered") >= 0)
                                    tool.ToolIsAttached = reader["ToolIsRegistered"].ToString();

                                tools.Add(tool);
                            }
                        }
                    }
                }
            } catch(Exception ex)
            {
                log.AddLogMessage("Problem getting SQL Server tools: " + ex.ToString());
            }

            return tools;
        }

        public bool TestConnection()
        {
            CheckAndCreateTable();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    log.AddLogMessage("Connected to SQL Server: " + connectionString);
                    return true;
                }
                catch(Exception ex)
                {
                    log.AddLogMessage("Problem connecting to SQL Server: " + ex.ToString());
                    return false;
                }
            }
        }

        public void UpdateTableStructure(ColumnSettings settings)
        {
            try
            {
                using (SQLiteConnection connection = CreateConnection())
                {
                    connection.Open();

                    List<string> existingColumns = GetTableColumns("Tools");

                    // Create a list for columns to be retained in the new table based on visibility settings
                    List<string> newColumns = new List<string>();

                    if (settings.ToolNumberVisible) newColumns.Add("ToolNumber INTEGER");
                    if (settings.PotNumVisible) newColumns.Add("PotNum INTEGER");
                    if (settings.ToolLifeVisible) newColumns.Add("ToolLife INTEGER");
                    if (settings.ToolLifeRemainingVisible) newColumns.Add("ToolLifeRem INTEGER");
                    if (settings.ToolNameVisible) newColumns.Add("ToolName TEXT");
                    if (settings.ToolLengthOffsetVisible) newColumns.Add("ToolLengthOffset INTEGER");
                    if (settings.ToolIsAttachedVisible) newColumns.Add("ToolIsRegistered TEXT");

                    // Create new table
                    string createTempTableQuery = $"CREATE TABLE Tools_temp ({string.Join(", ", newColumns)})";
                    new SQLiteCommand(createTempTableQuery, connection).ExecuteNonQuery();

                    // Get only the columns that are visible and exist in the database.
                    List<string> columnsToCopy = newColumns.Select(col => col.Split(' ')[0])
                                                        .Where(col => existingColumns.Contains(col))
                                                        .ToList();

                    // Construct the query to copy data.
                    string columnsStr = string.Join(", ", columnsToCopy);
                    string copyDataQuery = $"INSERT INTO Tools_temp ({columnsStr}) SELECT {columnsStr} FROM Tools";
                    new SQLiteCommand(copyDataQuery, connection).ExecuteNonQuery();

                    // Drop the old table
                    string dropOldTableQuery = "DROP TABLE Tools";
                    new SQLiteCommand(dropOldTableQuery, connection).ExecuteNonQuery();

                    // Rename the temporary table to the old table's name
                    string renameTempTableQuery = "ALTER TABLE Tools_temp RENAME TO Tools";
                    new SQLiteCommand(renameTempTableQuery, connection).ExecuteNonQuery();

                    connection.Close();
                }
            } catch(Exception ex)
            {
                log.AddLogMessage("Problem updating SQL Server table structure: " + connectionString + "\n" + ex.ToString());
            }
        }

        public List<string> GetTableColumns(string tableName)
        {
            List<string> columnNames = new List<string>();
            try
            {
                using (SQLiteConnection connection = CreateConnection())
                {
                    connection.Open();

                    // Get the SQL statement that created the table
                    string query = $"SELECT sql FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
                    {
                        string createTableSQL = (string)cmd.ExecuteScalar();

                        // Extract the part between the parentheses
                        int start = createTableSQL.IndexOf("(") + 1;
                        int end = createTableSQL.LastIndexOf(")");
                        string columnsPart = createTableSQL.Substring(start, end - start);

                        // Split the statement into individual columns
                        string[] columns = columnsPart.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string column in columns)
                        {
                            string columnName = column.Trim().Split(' ')[0];
                            columnNames.Add(columnName);
                        }
                    }

                    connection.Close();
                }
            } catch (Exception ex)
            {
                log.AddLogMessage("Problem getting SQL Sevrer table columns: " + connectionString + "\n" + ex.ToString());
            }
            return columnNames;
        }
    }
}
