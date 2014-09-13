using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;

[assembly: DebuggerVisualizer(typeof(ProjectVisualizer.GuidVisualizer),
typeof(VisualizerObjectSource),
Target = typeof(Guid),
Description = "Database Guid resolver")]
namespace ProjectVisualizer
{
    /// <summary>
    /// Attempts to query the database server and resolve a unique identifier to a user or group name.
    /// </summary>
    public class GuidVisualizer : DialogDebuggerVisualizer
    {
        private static SqlConnectionStringBuilder _connectionBuilder;

        /// <summary>
        /// List of known groups.
        /// </summary>
        private static Dictionary<Guid, string> _groups = null;

        /// <summary>
        /// List of known users.
        /// </summary>
        private static Dictionary<Guid, string> _users = null;

        /// <summary>
        /// Displays the <see cref="System.Guid"/> in a friendly manner.
        /// </summary>
        /// <param name="windowService">Window service.</param>
        /// <param name="objectProvider">Object to convert.</param>
        override protected void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            string result = String.Empty;

            try
            {
                if (_groups == null || _users == null)
                {
                    _groups = new Dictionary<Guid, string>();
                    _users = new Dictionary<Guid, string>();

                    List<KeyValuePair<Guid, String>> users = GetKeyValuePairs("[Security].[dbo].[Users]", "UserID", "UserName");

                    foreach (var kvp in users)
                    {
                        _users[kvp.Key] = kvp.Value;
                    }

                    List<KeyValuePair<Guid, String>> groups = GetKeyValuePairs("[Security].[dbo].[Groups]", "GroupID", "GroupName");

                    foreach (var kvp in groups)
                    {
                        _groups[kvp.Key] = kvp.Value;
                    }
                }

                Guid? obj = objectProvider.GetObject() as Guid?;

                if (obj.HasValue)
                {
                    Guid g = obj.Value;

                    if (_users.ContainsKey(g))
                    {
                        result = _users[g];
                    }
                    
                    // user might be null ... ?
                    if (String.IsNullOrEmpty(result) || _groups.ContainsKey(g))
                    {
                        result = _groups[g];
                    }
                }
            }
            catch (Exception ex)
            {
                result = "Exception querying database.\r\n";
                result += ex.Message;

                if (ex.InnerException != null)
                {
                    result += "\r\n";
                    result += "inner exception: \r\n";
                    result += ex.InnerException.Message;
                }
            }

            if (String.IsNullOrEmpty(result))
            {
                result = "Unknown";
            }

            MessageBox.Show(result);
        }

        /// <summary>
        /// Executes a SQL command. The results are available to a function which can parse results as needed.
        /// </summary>
        /// <param name="a">Action to perform on results.</param>
        /// <param name="query">SQL query to execute.</param>
        /// <remarks>
        /// Calling function must check for SQL injections, etc.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private void Execute(Action<SqlDataReader> a, string query)
        {
            if (_connectionBuilder == null)
            {
                InitConnectionBuilder();
            }

            using (SqlConnection sqlConnection1 = new SqlConnection(_connectionBuilder.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();

                reader = cmd.ExecuteReader();

                if (a != null)
                {
                    a(reader);
                }

                // connection is closed with using statement.
            }
        }

        /// <summary>
        /// Initializes SQL connection.
        /// </summary>
        private void InitConnectionBuilder()
        {
            _connectionBuilder = new SqlConnectionStringBuilder();

            // hard coded server name:
            _connectionBuilder["Data Source"] = "name_of_server";

			// use windows credentials
            _connectionBuilder["integrated Security"] = true;
            _connectionBuilder["Initial Catalog"] = "Security";
        }

        /// <summary>
        /// Selects two columns from the database. It is assumed these are key value pairs, where the key is a unique
        /// identifier and the value is a string.
        /// </summary>
        /// <param name="tableName">Fully qualified table name to select from.</param>
        /// <param name="keyColumnName">Name of unique identifier column.</param>
        /// <param name="valueColumnName">Name of value column.</param>
        /// <returns>A list of key value pairs from the table.</returns>
        private List<KeyValuePair<Guid, String>> GetKeyValuePairs(string tableName, string keyColumnName, string valueColumnName)
        {
            string query = String.Format("SELECT {1},{2} FROM {0}", tableName, keyColumnName, valueColumnName);

            List<KeyValuePair<Guid, String>> results = new List<KeyValuePair<Guid, string>>();

            Action<SqlDataReader> act = (reader) =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        results.Add(new KeyValuePair<Guid, string>(reader.GetGuid(0), reader.GetString(1)));
                    }
                }
            };

            Execute(act, query);

            return results;
        }
    }
}
