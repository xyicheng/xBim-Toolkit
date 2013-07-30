using System;
using System.Collections.Generic;
using System.Data;
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
            this.InitStructure();
        }

        public void InitStructure()
        {
            //Create the scene table
            this.ExecuteSQL("CREATE TABLE IF NOT EXISTS 'Scenes' (" +
                    "'SceneId' INTEGER PRIMARY KEY," +
                    "'SceneName' VARCHAR NOT NULL," +
                    "'BoundingBox' BLOB" +
                    ");");
            //create the layer table
            this.ExecuteSQL("CREATE TABLE IF NOT EXISTS 'Layers' (" +
                    "'SceneName' VARCHAR NOT NULL," +
                    "'LayerName' VARCHAR NOT NULL," +
                    "'LayerId' INTEGER PRIMARY KEY," +
                    "'ParentLayerId' INTEGER, " +
                    "'Meshes' BLOB, " +
                    "'XbimTexture' BLOB," +
                    "'BoundingBox' BLOB" +
                    ");");
            //create the meta data table
            this.ExecuteSQL("CREATE TABLE IF NOT EXISTS 'Meta' (" +
                    "'Meta_ID' INTEGER PRIMARY KEY," +
                    "'Meta_type' text not null," +
                    "'Meta_key' text," +
                    "'Meta_Value' text not null" +
                    ");");
        }

        /// <summary>
        /// Creates a table based on the SQL statement
        /// </summary>
        /// <param name="sql"></param>
        public void ExecuteSQL(string sql)
        {
            using (var mDBCon = this.GetConnection())
            {
                SQLiteCommand cmd = new SQLiteCommand(mDBCon);
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                mDBCon.Close();
            }
        }

        public enum DbConnMode
        {
            Open,
            Closed
        }

        public SQLiteConnection GetConnection(DbConnMode mode = DbConnMode.Open)
        {
            SQLiteConnection mDBcon = new SQLiteConnection();
            mDBcon.ConnectionString = this.ConnectionString;
            if (mode == DbConnMode.Open)
                mDBcon.Open();
            return mDBcon;
        }

        public void AddLayer(string layerName, int layerid, int parentLayerId, byte[] colour, byte[] vbo, byte[] bbArray)
        {
            string str = "INSERT INTO Layers (SceneName, LayerName, LayerId, ParentLayerId, Meshes, XbimTexture, BoundingBox) " +
                         "VALUES (@SceneName, @LayerName, @LayerId, @ParentLayerId, @Meshes, @XbimTexture, @BoundingBox) ";

            using (var mDBcon = this.GetConnection())
            {
                using (SQLiteTransaction SQLiteTrans = mDBcon.BeginTransaction())
                {
                    using (SQLiteCommand cmd = mDBcon.CreateCommand())
                    {
                        cmd.CommandText = str;
                        cmd.Parameters.Add("@SceneName", DbType.String).Value = "MainScene";
                        cmd.Parameters.Add("@LayerName", DbType.String).Value = layerName;
                        cmd.Parameters.Add("@LayerId", DbType.Int32).Value = layerid;
                        cmd.Parameters.Add("@ParentLayerId", DbType.Int32).Value = parentLayerId;
                        cmd.Parameters.Add("@Meshes", DbType.Binary).Value = vbo;
                        cmd.Parameters.Add("@XbimTexture", DbType.Binary).Value = colour;
                        cmd.Parameters.Add("@BoundingBox", DbType.Binary).Value = bbArray;
                        cmd.ExecuteNonQuery();
                    }

                    SQLiteTrans.Commit();
                }
            }
        }

        /// <summary>
        /// Adds a receord to the Meta table
        /// </summary>
        /// <param name="Type">Type of record (required)</param>
        /// <param name="Identifier">Optional</param>
        /// <param name="Value">Any string persistence mechanism of choice (required).</param>
        public void AddMetaData(string Type, string Value, string Identifier = null)
        {
            string str = "INSERT INTO Meta (" +
                "'Meta_type', 'Meta_Value', 'Meta_key' " +
                ") values (" +
                "@Meta_type, @Meta_Value, @Meta_key " +
                ")";

            using (var mDBcon = this.GetConnection())
            {
                using (SQLiteTransaction SQLiteTrans = mDBcon.BeginTransaction())
                {
                    using (SQLiteCommand cmd = mDBcon.CreateCommand())
                    {
                        cmd.CommandText = str;
                        cmd.Parameters.Add("@Meta_type", DbType.String).Value = Type;
                        cmd.Parameters.Add("@Meta_Value", DbType.String).Value = Value;
                        if (Identifier == null)
                            cmd.Parameters.Add("@Meta_key", DbType.String).Value = DBNull.Value;
                        else
                            cmd.Parameters.Add("@Meta_key", DbType.String).Value = Identifier;
                        cmd.ExecuteNonQuery();
                    }
                    SQLiteTrans.Commit();
                }
            }
        }
    }
}
