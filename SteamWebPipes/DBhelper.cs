using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamWebPipes
{
    internal class TableConfig
    {
        public TableOptions TableOptions { get; set; }
        public List<ColumnDef> Columns { get; set; }
    }

    internal class TableOptions
    {
        public string Charset { get; set; }
        public string Collation { get; set; }
    }

    internal class ColumnDef
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool NotNull { get; set; } = false;
        public bool PrimaryKey { get; set; } = false;
        public ForeignKeyDef ForeignKey { get; set; }
    }

    internal class ForeignKeyDef
    {
        public string Table { get; set; }
        public string Column { get; set; }
        public string OnDelete { get; set; }
        public string OnUpdate { get; set; }
    }

    public static class DBhelper
    {
        public static MySqlConnector.MySqlConnection GetConnection()
        {
            try
            {
                var db = new MySqlConnection(Bootstrap.Config.DatabaseConnectionString);
                return db;
            }
            catch (MySqlException e)
            {
                Bootstrap.Log("{0}", e.Message);
                return null;
            }
        }
        
        public static void InsertPICSEvent(IOrderedEnumerable<SteamChangelist> orderedList)
        {
            if (orderedList.Any())
            {
                List<SteamChangelist> list = [.. orderedList];

                using var connection = GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();
                foreach (var changeList in list)
                {
                    foreach (var appId in changeList.Apps)
                    {
                        long lastInsertedId = -1;
                        using (var cmd = new MySqlCommand(
                            "INSERT INTO pics_events (change_number, app_id, type) VALUES (@change_number, @app_id, @type)",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@change_number", changeList.ChangeNumber);
                            cmd.Parameters.AddWithValue("@app_id", appId);
                            //cmd.Parameters.AddWithValue("@type", changeList.Type);
                            cmd.ExecuteNonQuery();
                            lastInsertedId = cmd.LastInsertedId;
                        }

                        if (lastInsertedId > 0)
                        {
                            using var cmd = new MySqlCommand(
                                "INSERT INTO pics_event_job_status (event_id, job_id, status) " +
                                "SELECT @event_id, id, 'pending' FROM job_configs",
                               connection, transaction);
                            cmd.Parameters.AddWithValue("@event_id", lastInsertedId);
                            int rowsInserted = cmd.ExecuteNonQuery();
                        }

                    }
                }
                transaction.Commit();


            }
          
        }

        public static long GetLastChangeNumber()
        {
            long lastChangeNumber = 0;
            using (var connection = GetConnection())
            {
                connection.Open();
                string sql = "SELECT change_number FROM pics_events ORDER BY id DESC LIMIT 1;";
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        lastChangeNumber = Convert.ToInt64(result);
                }
            }

            return lastChangeNumber;
        }

        public static void InitilizeData()
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = "SELECT table_name, columns FROM job_configs";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var jobList = new List<(string TableName, string ColumnsJson)>();

            while (reader.Read())
            {
                jobList.Add((
                    reader.GetString("table_name"),
                    reader.GetString("columns")
                ));
            }

            reader.Close();

            foreach (var (TableName, ColumnsJson) in jobList)
            {
                // Parse JSON
                var tableConfig = JsonConvert.DeserializeObject<TableConfig>(ColumnsJson);
                if (tableConfig == null || tableConfig.Columns == null) continue;

                var defs = new List<string>();
                var pkList = new List<string>();
                var fkList = new List<string>();

                foreach (var col in tableConfig.Columns)
                {
                    var line = $"  `{col.Name}` {col.Type}";
                    if (col.NotNull) line += " NOT NULL";
                    defs.Add(line);

                    if (col.PrimaryKey)
                        pkList.Add($"`{col.Name}`");

                    if (col.ForeignKey != null)
                    {
                        var fk = $"  FOREIGN KEY (`{col.Name}`) REFERENCES `{col.ForeignKey.Table}`(`{col.ForeignKey.Column}`)";
                        if (!string.IsNullOrEmpty(col.ForeignKey.OnDelete))
                            fk += $" ON DELETE {col.ForeignKey.OnDelete}";
                        if (!string.IsNullOrEmpty(col.ForeignKey.OnUpdate))
                            fk += $" ON UPDATE {col.ForeignKey.OnUpdate}";
                        fkList.Add(fk);
                    }
                }

                if (pkList.Count != 0)
                    defs.Add($"  PRIMARY KEY ({string.Join(", ", pkList)})");

                defs.AddRange(fkList);

                // Default options
                string charset = tableConfig.TableOptions?.Charset ?? "utf8mb4";
                string collation = tableConfig.TableOptions?.Collation ?? "utf8mb4_unicode_ci";

                string createSql =
                    $"CREATE TABLE IF NOT EXISTS `{TableName}` (\n{string.Join(",\n", defs)}\n) " +
                    $"ENGINE=InnoDB DEFAULT CHARSET={charset} COLLATE={collation};";

                Bootstrap.Log($"Generated SQL for {TableName}:\n{createSql}\n");

                using var createCmd = new MySqlCommand(createSql, conn);
                createCmd.ExecuteNonQuery();

                Bootstrap.Log($"✅ Table {TableName} created/exists already.\n");
            }
        }
    }
}
