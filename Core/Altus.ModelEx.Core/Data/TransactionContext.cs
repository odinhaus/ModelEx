using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Diagnostics;
using Altus.Core.Diagnostics;
using System.IO;
using System.Threading;

namespace Altus.Core.Data
{
    public class TransactionContext
    {
        static bool _log = false;
        static string _logPath = "";
        static TransactionContext()
        {
            try
            {
                if (bool.TryParse(Context.GetEnvironmentVariable("MetaDataContextLogging").ToString(), out _log))
                {
                    _logPath = Path.Combine(Context.GlobalContext.CodeBase, "MetaDB.log");
                }
            }
            catch { }
        }

        public TransactionContext(DbTransaction transaction)
        {
            Transaction = transaction;
            HasErrors = false;
            IsFinalized = true;
            StackDepth = 0;
            if (_log)
            {
                ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Creating Transaction Context ===================");
            }
        }

        private string ThreadName()
        {
            return string.IsNullOrEmpty(Thread.CurrentThread.Name) ? Thread.CurrentThread.ManagedThreadId.ToString() : Thread.CurrentThread.Name;
        }

        public DbTransaction Transaction { get; private set; }
        public int StackDepth { get; private set; }
        public bool HasErrors { get; private set; }
        public bool IsFinalized { get; private set; }

        public void BeginTransaction() 
        { 
            StackDepth++; 
            IsFinalized = false;
            if (_log)
            {
                ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Incrementing Transaction Context: StackDepth: " + StackDepth + " ===================");
            }
        }
        public void CommitTransaction()
        {
            StackDepth--;
            if (_log)
            {
                ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Decrementing Transaction Context: StackDepth: " + StackDepth + " ===================");
            }
            if (StackDepth <= 0 && !HasErrors && !IsFinalized)
            {
                IsFinalized = true;
                if (_log)
                {
                    ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Committing Transaction Context ===================");
                }
                try
                {
                    DbConnection con = Transaction.Connection;
                    Transaction.Commit();
                    con.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "An error occurred while committing a transaction:");
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Error Occurred Committing Transaction Context: " + ex.ToString());
                    }
                }
                Transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            HasErrors = true;
            
            if (StackDepth > 0)
            {
                StackDepth--;
            }
            if (_log)
            {
                ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Rolling Back Transaction Context: StackDepth: " + StackDepth + " ===================");
            }

            if (!IsFinalized)
            {
                try
                {
                    DbConnection con = Transaction.Connection;
                    Transaction.Rollback();
                    con.Close();
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Rolled Back Transaction Context ===================");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "An error occurred rolling back a transaction:");
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, "Thread: " + ThreadName() + " - Error Occurred Rolling Back Transaction Context: " + ex.ToString());
                    }
                }
                Transaction = null;
                IsFinalized = true;
            }
        }

    }
}
