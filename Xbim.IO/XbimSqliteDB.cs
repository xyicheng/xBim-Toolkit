using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace Xbim.IO
{
    public class XbimSqliteDB
    {
        private string _dataBaseName;

        private string ConnectionString
        {
            get
            {
                return "Data Source=" + _dataBaseName + ";";
            }
        }
        public XbimSqliteDB(string databaseName)
        {
            _dataBaseName = databaseName;
        }

        /// <summary>
        /// Creates a table based on the SQL statement
        /// </summary>
        /// <param name="sql"></param>
        public void CreateTable(string sql )
        {
           
            SQLiteConnection mDBcon = new SQLiteConnection();
            mDBcon.ConnectionString = this.ConnectionString;
            mDBcon.Open();
            SQLiteCommand cmd = new SQLiteCommand(mDBcon);
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

        }

        public SQLiteConnection GetConnection()
        {
            SQLiteConnection mDBcon = new SQLiteConnection();
            mDBcon.ConnectionString = this.ConnectionString;
            mDBcon.Open();
            return mDBcon;
        }
    }
}
