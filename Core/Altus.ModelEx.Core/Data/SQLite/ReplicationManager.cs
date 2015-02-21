using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using moby.common.component;
using moby.common;
using moby.common.pubsub;
using moby.common.scheduling;
using moby.common.security;
using moby.common.system;
using moby.common.threading;
using moby.realtime;
using moby.common.datetime;

namespace moby.common.data.sqlite
{
    public class ReplicationManager : InitializableComponent, IScheduledTask, IReplicationManager
    {
        Topic _presence;
        protected override bool OnInitialize(params string[] args)
        {
            _presence = DataContext.Meta.GetTopic("CONNECT.System.Presence");
            if (_presence != null)
                _presence.Subscribe += presence_Subscribe;
            return true;
        }

        void presence_Subscribe(object sender, SubscriptionHandlerArgs e)
        {
            //foreach (Field f in e.Fields.Where(ff => ff.Value is Presence && ((Presence)ff.Value).NodeId != NodeIdentity.NodeId))
            //{
            //    HandlePresenceUpdate((Presence)f.Value);
            //}
        }

        private void HandlePresenceUpdate(Presence presence)
        {
            ulong localNodeId = 0;
            if (NodeIdentity.TryGetNodeId(presence.NodeAddress, out localNodeId))
            {
                presence.NodeId = localNodeId;
                DbState state = new DbState()
                {
                    Callback = new DbCallback(delegate(DbState s)
                        {
                            if (s.Reader.Read())
                            {
                                s.StateObject = s.Reader[0].ToString();
                            }
                            else
                            {
                                s.StateObject = null;
                            }
                        })
                };
                DataContext.Meta.ExecuteQuery(string.Format("SELECT SyncToken FROM repl_sync WHERE SourceNodeId = {0} and SinkNodeId = {1};", localNodeId, NodeIdentity.NodeId), state);
                if (state.StateObject == null)
                {
                    FullSyncExistingNode(presence);
                }
                else if (!presence.DbSyncToken.Equals(state.StateObject.ToString()))
                {
                    MergeSyncExistingNode(presence);
                }
            }
            else
            {
                FullSyncNewNode(presence);
            }
        }

        private void MergeSyncExistingNode(Presence presence)
        {
            
        }

        private void FullSyncNewNode(Presence presence)
        {
            
        }

        private void FullSyncExistingNode(Presence presence)
        {
            
        }

        public ReplicationManager(IDbConnectionManager mgr)
        {
            this.ConnectionManager = mgr;
            this.Schedule = new PeriodicSchedule(DateRange.Forever, 2000);
            MetaDataContextSQLite.SQLiteGUID.LastGUIDChanged += SQLiteGUID_LastGUIDChanged;
        }

        void SQLiteGUID_LastGUIDChanged(object sender, EventArgs e)
        {
            DbSyncToken = MetaDataContextSQLite.SQLiteGUID.LastGUID;
        }

        public string DbSyncToken { get; private set; }
        public IDbConnectionManager ConnectionManager { get; private set; }
        public System.Threading.ThreadPriority Priority { get { return ThreadPriority.Normal; } }
        public byte ProcessorAffinityMask { get; private set; }

        public void UpdateSchema()
        {
            using (DbLock theLock = this.ConnectionManager.CreateConnection(LockShare.Exclusive, LockOperation.Schema))
            {
                SQLiteSchema schema = new SQLiteSchema(this.ConnectionManager.Connection);

                //bool hasEvtTable = false;
                //bool hasSyncTable = false;
                //List<SQLiteObject> updateTokens = new List<SQLiteObject>();

                //foreach (SQLiteObject obj in schema.Objects)
                //{
                //    switch (obj.type)
                //    {
                //        case "table":
                //            {
                //                TableType type;
                //                SQLiteObject objRef = obj;
                //                HandleTable(ref objRef, theLock, out type);
                //                if (type == TableType.Event) hasEvtTable = true;
                //                if (type == TableType.Sync) hasSyncTable = true;
                //                if (objRef.updateToken) updateTokens.Add(objRef);
                //                break;
                //            }
                //        case "trigger":
                //            {
                //                SQLiteObject table = schema.Objects.Where(sqlo => sqlo.tbl_name == obj.tbl_name && sqlo.type == "table").FirstOrDefault();
                //                if (!string.IsNullOrEmpty(table.name))
                //                    table.Objects.Add(obj);
                //                break;
                //            }
                //    }
                //}

                //if (!hasEvtTable)
                //    CreateEventTable((SQLiteConnection)theLock.Connection);
                //if (!hasSyncTable)
                //    CreateSyncTable((SQLiteConnection)theLock.Connection);

                //foreach (SQLiteObject table in schema.Objects.Where(sqlo => sqlo.type == "table"))
                //{
                //    HandleTableTriggers(table, theLock);
                //}

                //foreach (SQLiteObject table in updateTokens)
                //    UpdateSyncToken(table, theLock);

                //SQLiteCommand cmd = new SQLiteCommand("SELECT SyncToken FROM repl_evt WHERE rowid = (SELECT MAX(rowid) FROM repl_evt);", 
                //    (SQLiteConnection)theLock.Connection);
                //object token = cmd.ExecuteScalar();
                //if (token == null || token == DBNull.Value)
                //{
                //    token = "";
                //}
                //this.DbSyncToken = token.ToString();
            }
        }

        private void HandleTableTriggers(SQLiteObject table, DbLock theLock)
        {
            if (table.tbl_name.ToLower().StartsWith("repl_")) return;

            if (table.Objects.Where(t => t.name.ToLower().Equals(string.Format("repl_{0}_delete", table.tbl_name.ToLower()))).Count() == 0)
                CreateDeleteTrigger(table, theLock);
            //if (table.Objects.Where(t => t.name.ToLower().Equals(string.Format("repl_{0}_insert", table.tbl_name.ToLower()))).Count() == 0)
            //    CreateInsertTrigger(table, theLock);
            if (table.Objects.Where(t => t.name.ToLower().Equals(string.Format("repl_{0}_update", table.tbl_name.ToLower()))).Count() == 0)
                CreateUpdateTrigger(table, theLock);

            foreach (SQLiteObject trigger in table.Objects.Where(o => o.type.ToLower().Equals("trigger")))
            {
                HandleTrigger(trigger, theLock);
            }
        }

        private void HandleTrigger(SQLiteObject obj, DbLock theLock)
        {
            string name = obj.name.ToLower();
            if (name.StartsWith("repl_") && name.EndsWith("_delete")) HandleDeleteTrigger(obj, theLock);
            //if (name.StartsWith("repl_") && name.EndsWith("_insert")) HandleInsertTrigger(obj, theLock);
            if (name.StartsWith("repl_") && name.EndsWith("_update")) HandleUpdateTrigger(obj, theLock);
        }

        private void HandleUpdateTrigger(SQLiteObject obj, DbLock theLock)
        {
            string sql = @"
                        CREATE TRIGGER [REPL_{0}_UPDATE] 
                        AFTER UPDATE ON [{0}] 
                        FOR EACH ROW WHEN OLD.repl_rowid = NEW.repl_rowid
                        BEGIN 

                        DELETE FROM repl_evt
                        WHERE repl_evt.SyncToken = OLD.repl_rowid
                        and repl_evt.EventType = 'UPDATE';

                        INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                        SELECT
                            CURRENT_TIMESTAMP,
                            '{0}',
                            'UPDATE',
                            repl_rowid,
                            hex(randomblob(18))
                        FROM {0}
                        WHERE ROWID = old.rowid;

                        END";
            if (!StripWhiteSpace(string.Format(sql, obj.tbl_name)).Equals(StripWhiteSpace(obj.sql), StringComparison.InvariantCultureIgnoreCase))
                CreateUpdateTrigger(obj, theLock);
        }

//        private void HandleInsertTrigger(SQLiteObject obj, DbLock theLock)
//        {
//            string sql = @"
//                        CREATE TRIGGER [REPL_{0}_INSERT] 
//                        AFTER INSERT ON [{0}] 
//                        FOR EACH ROW 
//                        BEGIN 
//
//                        INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
//                        SELECT
//                            CURRENT_TIMESTAMP,
//                            '{0}',
//                            'INSERT',
//                            repl_rowid,
//                            hex(randomblob(18))
//                        FROM {0}
//                        WHERE ROWID = new.rowid;
//
//                        END";
//            if (!StripWhiteSpace(string.Format(sql, obj.tbl_name)).Equals(StripWhiteSpace(obj.sql), StringComparison.InvariantCultureIgnoreCase))
//                CreateInsertTrigger(obj, theLock);
//        }

        private void HandleDeleteTrigger(SQLiteObject obj, DbLock theLock)
        {
            string sql = @"
                        CREATE TRIGGER [REPL_{0}_DELETE] 
                        AFTER DELETE ON [{0}] 
                        FOR EACH ROW 
                        BEGIN 

                        INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                        VALUES(CURRENT_TIMESTAMP, '{0}', 'DELETE', old.repl_rowid, hex(randomblob(18)));

                        END";
            if (!StripWhiteSpace(string.Format(sql, obj.tbl_name)).Equals(StripWhiteSpace(obj.sql), StringComparison.InvariantCultureIgnoreCase))
                CreateDeleteTrigger(obj, theLock);
        }

        private void CreateDeleteTrigger(SQLiteObject table, DbLock theLock)
        {
            string command = string.Format(@" 
                            DROP TRIGGER IF EXISTS [REPL_{0}_DELETE];
                            CREATE TRIGGER [REPL_{0}_DELETE] 
                            AFTER DELETE ON [{0}] 
                            FOR EACH ROW 
                            BEGIN 

                            INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                            VALUES(CURRENT_TIMESTAMP, '{0}', 'DELETE', old.repl_rowid, hex(randomblob(18)));

                            END;", table.tbl_name);
            SQLiteCommand cmd = new SQLiteCommand(command, (SQLiteConnection)theLock.Connection);
            cmd.ExecuteNonQuery();
        }

        private void CreateInsertTrigger(SQLiteObject table, DbLock theLock)
        {
            string command = string.Format(@" 
                            DROP TRIGGER IF EXISTS [REPL_{0}_INSERT];
                            CREATE TRIGGER [REPL_{0}_INSERT] 
                            AFTER INSERT ON [{0}] 
                            FOR EACH ROW 
                            BEGIN 

                            INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                            SELECT
                                CURRENT_TIMESTAMP,
                                '{0}',
                                'INSERT',
                                repl_rowid,
                                hex(randomblob(18))
                            FROM {0}
                            WHERE ROWID = new.rowid;

                            END;", table.tbl_name);
            SQLiteCommand cmd = new SQLiteCommand(command, (SQLiteConnection)theLock.Connection);
            cmd.ExecuteNonQuery();
        }

        private void CreateUpdateTrigger(SQLiteObject table, DbLock theLock)
        {
            string command = string.Format(@" 
                            DROP TRIGGER IF EXISTS [REPL_{0}_UPDATE];
                            CREATE TRIGGER [REPL_{0}_UPDATE] 
                            AFTER UPDATE ON [{0}] 
                            FOR EACH ROW WHEN OLD.repl_rowid = NEW.repl_rowid
                            BEGIN 

                            DELETE FROM repl_evt
                            WHERE repl_evt.SyncToken = OLD.repl_rowid
                            and repl_evt.EventType = 'UPDATE';

                            INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                            SELECT
                                CURRENT_TIMESTAMP,
                                '{0}',
                                'UPDATE',
                                repl_rowid,
                                hex(randomblob(18))
                            FROM {0}
                            WHERE ROWID = old.rowid;

                            END;", table.tbl_name);
            SQLiteCommand cmd = new SQLiteCommand(command, (SQLiteConnection)theLock.Connection);
            cmd.ExecuteNonQuery();
        }

        private string StripWhiteSpace(string source)
        {
            return source.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
        }

        private void UpdateSyncToken(SQLiteObject table, DbLock theLock)
        {
            string scriptName = string.Format("_ReplUpdate{0}RowToken", table.tbl_name);
            if (this.ConnectionManager.Connection.CommandScriptExists(scriptName))
            {
                this.ConnectionManager.Connection.DataContext.ExecuteScript(scriptName,
                    new DbParam("Table", table.tbl_name));
            }
            else
            {
                SQLiteCommand cmd = new SQLiteCommand(string.Format("UPDATE {0} SET repl_rowid = GUID();", table.tbl_name), (SQLiteConnection)theLock.Connection);
                cmd.ExecuteNonQuery();
            }
        }

        private void HandleTable(ref SQLiteObject obj, DbLock theLock, out TableType type)
        {
            if (obj.name == "repl_evt")
            {
                HandleReplEventTable(obj, theLock);
                type = TableType.Event;
            }
            else if (obj.name == "repl_sync")
            {
                HandleReplSyncTable(obj, theLock);
                type = TableType.Sync;
            }
            else
            {
                HandleDataTable(ref obj, theLock);
                type = TableType.Data;
            }
        }

        private void HandleDataTable(ref SQLiteObject obj, DbLock theLock)
        {
            if (obj.Replicate && !obj.HasReplToken)
            {
                AlterTableAddGuid(ref obj, theLock);
            }
        }

        private void AlterTableAddGuid(ref SQLiteObject obj, DbLock theLock)
        {
            string command = string.Format("ALTER TABLE [{0}]\r\nADD COLUMN [repl_rowid] varchar(128) NULL", obj.tbl_name);
            SQLiteCommand cmd = new SQLiteCommand(command, (SQLiteConnection)theLock.Connection);
            cmd.ExecuteNonQuery();
            obj.updateToken = true;
            cmd.CommandText = string.Format("CREATE UNIQUE INDEX [repl_rowid_idx_{0}] ON [{0}] ([repl_rowid]);", obj.tbl_name);
            cmd.ExecuteNonQuery();
        }

        private void HandleReplSyncTable(SQLiteObject obj, DbLock theLock)
        {
            if (obj.Columns.Where(c => (c.name.ToLower().Equals("sourcenodeid") && c.type.ToLower().Equals("integer"))
                || (c.name.ToLower().Equals("sinknodeid") && c.type.ToLower().Equals("integer"))
                || (c.name.ToLower().Equals("synctoken") && c.type.ToLower().Equals("varchar(36)"))).Count() != 2)
            {
                CreateSyncTable((SQLiteConnection)theLock.Connection);
            }
        }

        private void HandleReplEventTable(SQLiteObject obj, DbLock theLock)
        {
            if (obj.Columns.Where(c => (c.name.ToLower().Equals("id") && c.type.ToLower().Equals("integer"))
                || (c.name.ToLower().Equals("timestamp") && c.type.ToLower().Equals("timestamp"))
                || (c.name.ToLower().Equals("targettable") && c.type.ToLower().Equals("varchar(64)"))
                || (c.name.ToLower().Equals("eventtype") && c.type.ToLower().Equals("varchar(16)"))
                || (c.name.ToLower().Equals("RowToken") && c.type.ToLower().Equals("varchar(36)"))
                || (c.name.ToLower().Equals("synctoken") && c.type.ToLower().Equals("varchar(36)"))
                ).Count() != 5)
            {
                CreateEventTable((SQLiteConnection)theLock.Connection);
            }
        }

        private void CreateSyncTable(SQLiteConnection connection)
        {
            string command = "DROP TABLE IF EXISTS [repl_sync];\r\n"
                + "CREATE TABLE [repl_sync] ("
                + "[SourceNodeId] INTEGER NOT NULL, "
                + "[SinkNodeId] INT NOT NULL, "
                + "[SyncToken] VARCHAR(36) NOT NULL, "
                + "CONSTRAINT [] PRIMARY KEY ([SourceNodeId], [SinkNodeId]) ON CONFLICT FAIL);";
            SQLiteCommand cmd = new SQLiteCommand(command, connection);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE INDEX [repl_sync_token_idx] ON [repl_sync] ([SyncToken]);";
            cmd.ExecuteNonQuery();
        }

        private void CreateEventTable(SQLiteConnection connection)
        {
            string command = "DROP TABLE IF EXISTS [repl_evt];\r\n"
                + "CREATE TABLE [repl_evt] (\r\n[Id] INTEGER  PRIMARY KEY AUTOINCREMENT NOT NULL,\r\n"
                + "[Timestamp] TIMESTAMP  NOT NULL,\r\n"
                + "[TargetTable] VARCHAR(64)  NOT NULL,\r\n"
                + "[EventType] VARCHAR(16)  NOT NULL,\r\n"
                + "[RowToken] VARCHAR(36) NOT NULL,\r\n"
                + "[SyncToken] VARCHAR(36)  NOT NULL\r\n)";
            SQLiteCommand cmd = new SQLiteCommand(command, connection);
            cmd.ExecuteNonQuery();
        }

        public object Execute(params object[] args)
        {
            CheckDbState();
            return args;
        }

        private void CheckDbState()
        {
            /*
                DELETE FROM repl_evt
                WHERE rowid <= (
                SELECT Min(E.rowid)
                FROM repl_evt E
                JOIN repl_sync S on S.[SyncToken] = E.[SyncToken])
             */
        }

        public Schedule Schedule
        {
            get;
            set;
        }

        public void Kill()
        {
            this.Schedule = new PeriodicSchedule(new datetime.DateRange(DateTime.MinValue, DateTime.MinValue.AddTicks(1)), 0);
        }
    }
}
