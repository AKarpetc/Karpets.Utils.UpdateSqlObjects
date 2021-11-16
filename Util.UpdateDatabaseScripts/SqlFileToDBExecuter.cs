using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Util.UpdateDatabaseScripts
{
    public class SqlFileToDBExecuter : IFileExecuter
    {
        private readonly string _conectionString;

        private const string ALTER = "ALTER ";
        private const string CREATE = "CREATE ";

        public SqlFileToDBExecuter(string conectionString)
        {
            _conectionString = conectionString;
        }

        public List<string> ProductOnlyCollection { get; set; }

        private T SQLQuery<T>(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    return reader.GetFieldValue<T>(0);
                }
            }
        }

        private void SQLExec(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        public void Execute(ExecutingModel model)
        {
            using (SqlConnection connection = new SqlConnection(_conectionString))
            {
                connection.Open();

                UpdateScript(model.Name, model.Content, model.Folder, model.Type, connection);

                connection.Close();
            }
        }

        private string UpdateScript(string name, string text, string folder, string type, SqlConnection connection)
        {
            string checkedIfExist = "";

            if (type.Trim().ToUpper() != "FUNCTION")
            {
                checkedIfExist = $"select count(*) FROM sys.{folder} where name = '{name}'";
            }
            else
            {
                checkedIfExist = @$"SELECT COUNT(*)
                  FROM sys.sql_modules m 
                  INNER JOIN sys.objects o ON m.object_id=o.object_id
                  WHERE type_desc like '%function%' and name='{name}'";
            }

            var count = SQLQuery<int>(connection, checkedIfExist);

            var pattern = @"(CREATE|ALTER) *" + type;

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var replaceTo = "";

            if (count == 0)
            {
                replaceTo = CREATE + type.ToUpper();
            }
            else
            {
                replaceTo = ALTER + type.ToUpper();
            }

            text = regex.Replace(text, replaceTo);

            SQLExec(connection, text);

            return text;
        }
    }
}
