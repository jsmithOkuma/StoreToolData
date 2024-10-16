using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace StoreToolData
{
    class SQLServerOperations : IDatabaseOperations
    {
        private string connectionString;
        Log log;

        public SQLServerOperations(string connStr)
        {
            this.connectionString = connStr;
            log = new Log();

            log.AddLogMessage("Initializing SQL Server operations: " + connStr);
        }

        public void CheckAndCreateTable()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string checkTableExistsQuery = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Tools') " +
                              "CREATE TABLE Tools (" +
                              "ToolNumber INT, " +
                              "PotNum INT, " +
                              "ToolLife INT, " +
                              "ToolLifeRem INT, " +
                              "ToolName NVARCHAR(255), " +
                              "ToolLengthOffset FLOAT, " +
                              "ToolIsRegistered NVARCHAR(255)" +
                              ")";

                    using (SqlCommand cmd = new SqlCommand(checkTableExistsQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
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
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    List<string> columns = GetTableColumns("Tools");

                    // Step 1: Fetch all tool numbers from the database
                    List<int> dbToolNumbers = new List<int>();
                    using (SqlCommand cmdFetch = new SqlCommand("SELECT ToolNumber FROM Tools", connection))
                    {
                        using (SqlDataReader reader = cmdFetch.ExecuteReader())
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

                        using (SqlCommand cmdCheck = new SqlCommand(checkExistQuery, connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@ToolNumber", tool.ToolNumber);
                            using (SqlDataReader reader = cmdCheck.ExecuteReader())
                            {
                                toolExists = reader.HasRows;
                                reader.Close();
                            }
                        }

                        // Step 2: Remove tool from dbToolNumbers if it exists
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

                            updateBuilder.Append("WHERE ToolNumber = @ToolNumber");

                            using (SqlCommand cmdUpdate = new SqlCommand(updateBuilder.ToString().Trim().TrimEnd(','), connection))
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
                            if (columns.Contains("ToolIsRegistered")) insertBuilder.Append("ToolIsRegistered");

                            insertBuilder.Append(") VALUES (");
                            if (columns.Contains("ToolNumber")) insertBuilder.Append("@ToolNumber, ");
                            if (columns.Contains("PotNum")) insertBuilder.Append("@PotNum, ");
                            if (columns.Contains("ToolLife")) insertBuilder.Append("@ToolLife, ");
                            if (columns.Contains("ToolLifeRem")) insertBuilder.Append("@ToolLifeRem, ");
                            if (columns.Contains("ToolName")) insertBuilder.Append("@ToolName, ");
                            if (columns.Contains("ToolLengthOffset")) insertBuilder.Append("@ToolLengthOffset, ");
                            if (columns.Contains("ToolIsRegistered")) insertBuilder.Append("@ToolIsRegistered");

                            string insertQuery = insertBuilder.ToString().Trim().TrimEnd(',') + ")";

                            using (SqlCommand cmdInsert = new SqlCommand(insertQuery, connection))
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

                    // Step 3: Delete old tools not present in the provided list
                    foreach (int oldToolNumber in dbToolNumbers)
                    {
                        using (SqlCommand cmdDelete = new SqlCommand("DELETE FROM Tools WHERE ToolNumber = @ToolNumber", connection))
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

        public List<string> GetTableColumns(string tableName)
        {
            List<string> columns = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@TableName", tableName);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add(reader["COLUMN_NAME"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLogMessage("Problem getting SQL Sevrer table columns: " + connectionString + "\n" + ex.ToString());
            }

            return columns;
        }

        public void UpdateTableStructure(ColumnSettings settings)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check for existing columns in the database
                    string getColumnQuery = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Tools'";
                    SqlCommand cmd = new SqlCommand(getColumnQuery, connection);
                    List<string> existingColumns = new List<string>();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingColumns.Add(reader["COLUMN_NAME"].ToString());
                        }
                    }

                    // ToolNumber Column Handling
                    if (!settings.ToolNumberVisible && existingColumns.Contains("ToolNumber"))
                    {
                        string dropColumnQuery = "ALTER TABLE Tools DROP COLUMN ToolNumber";
                        new SqlCommand(dropColumnQuery, connection).ExecuteNonQuery();
                    }
                    else if (settings.ToolNumberVisible && !existingColumns.Contains("ToolNumber"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolNumber INT";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // PotNum Column Handling
                    if (!settings.PotNumVisible && existingColumns.Contains("PotNum"))
                    {
                        string dropColumnQuery = "ALTER TABLE Tools DROP COLUMN PotNum";
                        new SqlCommand(dropColumnQuery, connection).ExecuteNonQuery();
                    }
                    else if (settings.PotNumVisible && !existingColumns.Contains("PotNum"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD PotNum INT";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // ToolLife Column Handling
                    if (!settings.ToolLifeVisible && existingColumns.Contains("ToolLife"))
                    {
                        string dropColumnQuery = "ALTER TABLE Tools DROP COLUMN ToolLife";
                        new SqlCommand(dropColumnQuery, connection).ExecuteNonQuery();
                    }
                    else if (settings.ToolLifeVisible && !existingColumns.Contains("ToolLife"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolLife INT";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // ToolLifeRem Column Handling
                    if (!settings.ToolLifeRemainingVisible && existingColumns.Contains("ToolLifeRem"))
                    {
                        string dropColumnQuery = "ALTER TABLE Tools DROP COLUMN ToolLifeRem";
                        new SqlCommand(dropColumnQuery, connection).ExecuteNonQuery();
                    }
                    else if (settings.ToolLifeRemainingVisible && !existingColumns.Contains("ToolLifeRem"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolLifeRem INT";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // ToolLengthOffset Column Handling
                    if (!existingColumns.Contains("ToolLengthOffset"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolLengthOffset FLOAT";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // ToolIsRegistered Column Handling
                    if (!existingColumns.Contains("ToolIsRegistered"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolIsRegistered NVARCHAR(255)";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }

                    // ToolName Column Handling
                    if (!settings.ToolNameVisible && existingColumns.Contains("ToolName"))
                    {
                        string dropColumnQuery = "ALTER TABLE Tools DROP COLUMN ToolName";
                        new SqlCommand(dropColumnQuery, connection).ExecuteNonQuery();
                    }
                    else if (settings.ToolNameVisible && !existingColumns.Contains("ToolName"))
                    {
                        string addColumnQuery = "ALTER TABLE Tools ADD ToolName NVARCHAR(255)";
                        new SqlCommand(addColumnQuery, connection).ExecuteNonQuery();
                    }
                }
            } catch (Exception ex)
            {
                log.AddLogMessage("Problem updating SQL Server table structure: " + connectionString + "\n" + ex.ToString());
            }
            
        }

        public List<Tool> GetTools()
        {
            List<Tool> tools = new List<Tool>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    List<string> columns = GetTableColumns("Tools");

                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Tools", connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Tool tool = new Tool();

                                if (columns.Contains("ToolNumber"))
                                    tool.ToolNumber = Convert.ToInt32(reader["ToolNumber"]);

                                if (columns.Contains("PotNum"))
                                    tool.PotNum = Convert.ToInt32(reader["PotNum"]);

                                if (columns.Contains("ToolLife"))
                                    tool.ToolLife = Convert.ToInt32(reader["ToolLife"]);

                                if (columns.Contains("ToolLifeRem"))
                                    tool.ToolLifeRem = Convert.ToInt32(reader["ToolLifeRem"]);

                                if (columns.Contains("ToolLengthOffset"))
                                    tool.ToolLengthOffset = Convert.ToDouble(reader["ToolLengthOffset"]);

                                if (columns.Contains("ToolIsRegistered"))
                                    tool.ToolIsAttached = reader["ToolIsRegistered"].ToString();

                                if (columns.Contains("ToolName"))
                                    tool.ToolName = reader["ToolName"].ToString();

                                tools.Add(tool);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AddLogMessage("Problem getting SQL Server tools: " + ex.ToString());
            }

            return tools;
        }

        public bool TestConnection()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
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
    }
}
