using Npgsql;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebHookExample
{
    public static class Modulo
    {
        public static string ID_loginUser = /*(System.Security.Principal.WindowsIdentity.GetCurrent().Name);*/ Environment.UserName;
        public static string pCONECTION_ID = "string de conexão"


        static public int SQL_executeNonQuery(NpgsqlCommand Command)
        {
            var conn = new NpgsqlConnection(pCONECTION_ID);
            object objReturn = null;
            Command.CommandText = Command.CommandText.Insert(0, "set session \"myapp.user\" = '" + ID_loginUser + "';");
            int Result = 1;

            NpgsqlTransaction mytrans = null;
            try
            {
                conn.Open();
                mytrans = conn.BeginTransaction();
                Command.Transaction = mytrans;
                Command.Connection = conn;
                objReturn = Command.ExecuteNonQuery();
                mytrans.Commit();
                conn.Close();
            }
            catch (Exception e)
            {
                if (mytrans != null)
                {
                    mytrans.Rollback();
                }
                Console.WriteLine("Problemas no SQL - MSG:" + e.Message);
                conn.Close();
                Result = 0;
            }

            return Result;
        }

        public static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }

        static public DataTable SQL_GetDatatable(string cSelectCommand)
        {
            var conn = new NpgsqlConnection(pCONECTION_ID);
            var objReturn = new DataTable();
            string Command = "set session \"myapp.user\" = '" + ID_loginUser + "';";
            Command += cSelectCommand;
            var da = new NpgsqlDataAdapter(Command, conn);
            da.Fill(objReturn);
            return objReturn;
        }

        public static DateTime GetUnixToDate(double unixTimeStamp)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return unixEpoch.AddSeconds(unixTimeStamp - 10800);
        }

    }
}
