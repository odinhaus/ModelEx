using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Altus.Core.Diagnostics;
using Altus.Core.Exceptions;
using Altus.Core;
using Altus.Core.Component;

namespace Altus.Core.Caching
{
    public class WinCache : ICache
    {
        #region Fields
        #region Static Fields
        static Dictionary<string, object> _cache = new Dictionary<string, object>();
        static Thread _timeoutThread;
        static Dictionary<string, System.DateTime> _absolutes = new Dictionary<string, System.DateTime>();
        static Dictionary<string, TimeSpan> _inactivity = new Dictionary<string, TimeSpan>();
        static Dictionary<string, List<string>> _dependencies = new Dictionary<string, List<string>>();
        static Dictionary<string, System.DateTime> _lastAccess = new Dictionary<string, System.DateTime>();
        static TimeSpan _pollInterval = TimeSpan.FromSeconds(90);
        static ManualResetEvent _wait = new ManualResetEvent(true);
        static object _sync = new object();
        #endregion Static Fields

        #region Instance Fields
        #endregion Instance Fields
        #endregion Fields

        #region Constructors
        #region Public
        public WinCache()
        {

        }
        #endregion
        #region Private
        static WinCache()
        {
            _timeoutThread = new Thread(new ThreadStart(ExpirationLoop));
            _timeoutThread.IsBackground = true;
            _timeoutThread.Name = "WinCache Expiration Thread";
            _timeoutThread.Start();
        }

        #endregion Private

        #endregion  Constructors

        #region Properties
        #region Public
        public object SyncRoot
        {
            get { return _sync; }
        }
        public static TimeSpan PollInterval
        {
            get { return _pollInterval; }
            set
            {
                _pollInterval = value;
                _wait.Set();
            }
        }
        public int Count
        {
            get { return _cache.Count; }
        }

        public object this[string key]
        {
            get
            {
                try
                {
                    Monitor.Enter(_sync);
                    if (_cache.ContainsKey(key))
                    {

                        _lastAccess[key] = CurrentTime.Now;


                        return _cache[key];
                    }
                    else return null;
                }
                finally
                {
                    Monitor.Exit(_sync);
                }
            }
        }
        #endregion Public
        #region Private
        private Context _ctx
        {
            get { return Context.CurrentContext; }
        }
        #endregion
        #endregion Properties

        #region Methods
        #region Public
        public void Add(string key, object value)
        {
            this.Add(key, value, DateTime.MaxValue, TimeSpan.MaxValue, new string[] { });
        }

        public void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, string[] dependencies)
        {
            try
            {
                Monitor.Enter(_sync);
                if (_cache.ContainsKey(key))
                {
                    _cache[key] = value;
                    if (absoluteExpiration != DateTime.MinValue)
                    {
                        _absolutes[key] = absoluteExpiration;
                        _inactivity[key] = TimeSpan.MaxValue;
                    }
                    else
                    {
                        _absolutes[key] = DateTime.MaxValue;
                        _inactivity[key] = slidingExpiration;
                    }
                    if (dependencies == null) dependencies = new string[] { };
                    _dependencies[key] = new List<string>(dependencies);
                    _lastAccess[key] = CurrentTime.Now;
                }
                else
                {
                    _cache.Add(key, value);

                    if (absoluteExpiration != DateTime.MinValue)
                    {
                        _absolutes.Add(key, absoluteExpiration);
                        _inactivity.Add(key, TimeSpan.MaxValue);
                    }
                    else
                    {
                        _absolutes.Add(key, DateTime.MaxValue);
                        _inactivity.Add(key, slidingExpiration);
                    }
                    if (dependencies == null) dependencies = new string[] { };
                    _dependencies.Add(key, new List<string>(dependencies));
                    _lastAccess.Add(key, CurrentTime.Now);
                }
            }
            finally
            {
                Monitor.Exit(_sync);
            }
        }

        public void Insert(string key, object value)
        {
            this.Insert(key, value, DateTime.MaxValue, TimeSpan.MaxValue, new string[] { });
        }

        public void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, string[] dependencies)
        {
            try
            {
                Monitor.Enter(_sync);
                if (_cache.ContainsKey(key))
                {
                    _cache[key] = value;

                    if (absoluteExpiration != DateTime.MinValue)
                    {
                        _absolutes[key] = absoluteExpiration;
                        _inactivity[key] = TimeSpan.MaxValue;
                    }
                    else
                    {
                        _absolutes[key] = DateTime.MaxValue;
                        _inactivity[key] = slidingExpiration;
                    }
                    if (dependencies == null) dependencies = new string[] { };

                    _dependencies[key] = new List<string>(dependencies);
                    _lastAccess[key] = CurrentTime.Now;
                }
                else
                {
                    _cache.Add(key, value);

                    if (absoluteExpiration != DateTime.MinValue)
                    {
                        _absolutes.Add(key, absoluteExpiration);
                        _inactivity.Add(key, TimeSpan.MaxValue);
                    }
                    else
                    {
                        _absolutes.Add(key, DateTime.MaxValue);
                        _inactivity.Add(key, slidingExpiration);
                    }

                    _dependencies.Add(key, new List<string>(dependencies));
                    _lastAccess.Add(key, CurrentTime.Now);
                }
            }
            finally
            {
                Monitor.Exit(_sync);
            }
        }

        public void Remove(string key)
        {
            Monitor.Enter(_sync);
            RemoveItem(key);
            Monitor.Exit(_sync);
        }
        #endregion Public

        #region Private
        static void ExpirationLoop()
        {
            try
            {
                while (true)
                {
                    _wait.WaitOne(_pollInterval, false);
                    if (Monitor.TryEnter(_sync, 200)) // we try for a lock for 200ms, if we don't get it, we go back to sleep
                    {
                        try
                        {
                            Dictionary<string, object>.Enumerator oEnum = _cache.GetEnumerator();
                            List<string> removes = new List<string>();
                            bool flag = false;

                            while (oEnum.MoveNext())
                            {
                                // check absolute expiration
                                if (_absolutes.ContainsKey(oEnum.Current.Key))
                                {
                                    DateTime time = _absolutes[oEnum.Current.Key];
                                    if (time < CurrentTime.Now)
                                    {
                                        removes.Add(oEnum.Current.Key);
                                        flag = true;
                                    }
                                }
                                // check expired dependencies
                                if (!flag)
                                {
                                    List<string> deps = _dependencies[oEnum.Current.Key];
                                    if (deps != null && deps.Count > 0)
                                    {
                                        for (int i = 0; i < deps.Count; i++)
                                        {
                                            if (!_cache.ContainsKey(deps[i])
                                                || removes.Contains(deps[i]))
                                            {
                                                removes.Add(oEnum.Current.Key);
                                                flag = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                // check last access expiration
                                if (!flag)
                                {
                                    if (_inactivity.ContainsKey(oEnum.Current.Key))
                                    {
                                        DateTime time = _lastAccess[oEnum.Current.Key];
                                        TimeSpan span = _inactivity[oEnum.Current.Key];
                                        DateTime checkTime = span < TimeSpan.MaxValue ? time.Add(span) : DateTime.MaxValue;
                                        if (checkTime < CurrentTime.Now)
                                        {
                                            removes.Add(oEnum.Current.Key);
                                        }
                                    }

                                }
                                else
                                {
                                    flag = true;
                                }
                            }

                            foreach (string key in removes)
                            {
                                RemoveItem(key);
                            }
                        }
                        finally
                        {
                            /// ROK 4/25/07 - Removed from outer catch block and modified this to add the try/finally
                            /// block here because on teardown this sync context was being released twice, once here 
                            /// in the original code, and again below in a finally block following the catch 
                            /// ThreadAbortException...
                            Monitor.Exit(_sync);
                        }
                    } // end if (Monitor.TryEnter(_sync, 200))

                    _wait.Reset();
                } // end while( true )
            }
            catch (ThreadAbortException tae)
            {
                if (App.Instance != null && App.Instance.IsRunning)
                    Logger.LogWarn(ExceptionCode.SystemWinCacheExpirationThreadExited, tae.Message);
            }
            catch (Exception ex)
            {
                /// ROK 4/25/07 - Removed as described above
                /// Monitor.Exit(_sync);
                Logger.LogError(ExceptionCode.SystemWinCacheExpirationThreadExited, ex.Message);
            }
        }

        static void RemoveItem(string key)
        {
            if (_cache.ContainsKey(key)) _cache.Remove(key);
            if (_absolutes.ContainsKey(key)) _absolutes.Remove(key);
            if (_inactivity.ContainsKey(key)) _inactivity.Remove(key);
            if (_lastAccess.ContainsKey(key)) _lastAccess.Remove(key);
            if (_dependencies.ContainsKey(key)) _dependencies.Remove(key);
            Console.WriteLine("Cached item " + key + " has been removed.");
        }
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Methods

        #region Event Handlers and Callbacks
        #region Public
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Event Handlers and Callbacks


        #region ICache Members


        public bool ContainsKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        #endregion
    }
}
