using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Altus.Core.Threading;

namespace Altus.Core.Data.SQlite
{
    public struct SQLiteObject
    {
        public string type;
        public string name;
        public string tbl_name;
        public string sql;
        public List<SQLiteObject> Objects;
        public bool updateToken;
        public List<SQLiteColumn> Columns;
        public bool HasReplToken;
        public bool Replicate;
        public string ReplicationQuery;
    }

    public struct SQLiteColumn
    {
        public long cid;
        public string name;
        public string type;
        public bool pk;
        public bool unique;
        public SQLiteForeignKey reference;
        public bool HasReference { get { return this.reference.table != null; } }
    }

    public struct SQLiteForeignKey
    {
        public string table;
        public string column;
    }

    public enum TableType
    {
        Data,
        Event,
        Sync
    }

    public class SQLiteSchema
    {
        static Dictionary<string, Dictionary<string, SQLiteObject>> _cache = new Dictionary<string, Dictionary<string, SQLiteObject>>();
        public SQLiteSchema(IDbConnection connection)
        {
            this.Connection = connection;
            GetSchema();
        }

        public IDbConnection Connection { get; private set; }

        Dictionary<string, SQLiteObject> _schema = new Dictionary<string, SQLiteObject>();
        public SQLiteObject this[int index]
        {
            get { return _schema.Values.ToArray()[index]; }
        }

        public SQLiteObject this[string objName]
        {
            get { return _schema[objName.ToLower()]; }
        }

        public IEnumerable<SQLiteObject> Objects { get { return _schema.Values.ToArray(); } }

        private void GetSchema()
        {
            lock (this)
            {
                if (_cache.ContainsKey(this.Connection.ConnectionString))
                {
                    _schema = _cache[this.Connection.ConnectionString];
                }
                else
                {
                    using (DbLock theLock = this.Connection.ConnectionManager.CreateConnection(LockShare.Exclusive, LockOperation.Schema))
                    {
                        SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM sqlite_master", (SQLiteConnection)theLock.Connection);
                        cmd.CommandType = System.Data.CommandType.Text;

                        List<SQLiteObject> objects = new List<SQLiteObject>();
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (!rdr["name"].ToString().ToLower().StartsWith("sqlite"))
                                {
                                    objects.Add(new SQLiteObject()
                                    {
                                        type = rdr["type"].ToString(),
                                        name = rdr["name"].ToString(),
                                        tbl_name = rdr["tbl_name"].ToString(),
                                        sql = rdr["sql"].ToString(),
                                        Objects = new List<SQLiteObject>()
                                    });
                                }
                            }
                        }


                        foreach (SQLiteObject obj in objects)
                        {
                            switch (obj.type)
                            {
                                case "table":
                                    {
                                        TableType type;
                                        SQLiteObject objRef = obj;
                                        HandleTable(theLock, ref objRef, out type);
                                        _schema.Add(objRef.name.ToLower(), objRef);
                                        break;
                                    }
                                default:
                                    {
                                        _schema.Add(obj.name.ToLower(), obj);
                                        break;
                                    }
                            }
                        }
                    }
                    _cache.Add(this.Connection.ConnectionString, _schema);
                }
            }
        }

        Regex _reference = new Regex(@"(?<FK>\[[\w\s]+\])[\w\s\(\)]+REFERENCES\s+(?<Table>\[[\w\s]+\])\s*\((?<PK>\[[\w\s]+\])\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex _uniqueMulti = new Regex(@"UNIQUE\s*\((?<Fields>[\w\,\[\]\s]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex _uniqueSingle = new Regex(@"(?<Field>\[[\w\s]+\])[\w\s\(\)]+UNIQUE", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private void HandleTable(DbLock theLock, ref SQLiteObject obj, out TableType type)
        {
            // PRAGMA table_info(Calculator)
            /*
            cid	name	        type	        notnull	dflt_value	        pk
            0	Id	            INTEGER	        99		                    1
            1	EventId	        INTEGER	        99		                    0
            2	TargetObjectId	INTEGER	        99		                    0
            3	TargetFieldId	INTEGER	        99		                    0
            4	Name	        VARCHAR(32)	    99		                    0
            5	Condition	    VARCHAR(256)	99	    ''''''true'''''''	0
            6	Formula	        VARCHAR(256)	99		                    0
            7	Enabled	        BOOLEAN	        99		                    0
            8	Guid	        VARCHAR(36)	    0		                    0
            */
            Dictionary<string, SQLiteForeignKey> fks = new Dictionary<string, SQLiteForeignKey>();
            Match m = _reference.Match(obj.sql); //TODO:  make the regex support composite keys like   FOREIGN KEY([FieldId], [ObjectId]) REFERENCES [Field]([Id], [ObjectId])

            while (m.Success)
            {
                fks.Add(m.Groups["FK"].Value.Replace("[", "").Replace("]", "").ToLower(),
                    new SQLiteForeignKey()
                    {
                        column = m.Groups["PK"].Value.Replace("[", "").Replace("]", ""),
                        table = m.Groups["Table"].Value.Replace("[", "").Replace("]", "")
                    });
                m = m.NextMatch();
            }

            string sql = obj.sql;

            List<string> columns = new List<string>();
            m = _uniqueMulti.Match(sql);
            while (m.Success)
            {
                columns.AddRange(m.Groups["Fields"].Value.Split(','));
                m = m.NextMatch();
            }

            m = _uniqueSingle.Match(sql);
            while (m.Success)
            {
                columns.Add(m.Groups["Field"].Value);
                m = m.NextMatch();
            }

            SQLiteCommand cmd = new SQLiteCommand(string.Format("PRAGMA table_info({0});", obj.tbl_name), (SQLiteConnection)theLock.Connection);
            bool hasGuid = false;
            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                obj.Columns = new List<SQLiteColumn>();
                while (rdr.Read())
                {
                    if (rdr["name"].ToString().ToLower() == "repl_rowid"
                        && rdr["type"].ToString().ToLower() == "varchar(128)")
                    {
                        hasGuid = true;
                        break;
                    }
                    obj.Columns.Add(new SQLiteColumn()
                    {
                        cid = (long)rdr["cid"],
                        name = rdr["name"].ToString(),
                        type = rdr["type"].ToString(),
                        pk = (long)rdr["pk"] == 1 ? true : false,
                        reference = fks.ContainsKey(rdr["name"].ToString().ToLower()) ? fks[rdr["name"].ToString().ToLower()] : new SQLiteForeignKey(),
                        unique = columns.Count(c => c.Replace("[","").Replace("]","").Equals(rdr["name"].ToString(), StringComparison.InvariantCultureIgnoreCase)) == 1
                    });
                }
            }
            obj.HasReplToken = hasGuid;
            if (obj.name == "repl_evt")
            {
                type = TableType.Event;
                obj.Replicate = false;
            }
            else if (obj.name == "repl_sync")
            {
                type = TableType.Sync;
                obj.Replicate = false;
            }
            else
            {
                type = TableType.Data;
                obj.Replicate = true;
            }
        }
    }
}
