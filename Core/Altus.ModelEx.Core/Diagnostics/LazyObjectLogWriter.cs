using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Altus.Core.Streams;
using Altus.Core.Configuration;
using Altus.Core.Serialization;
using Altus.Core.Compression;
using Altus.Core.Threading;
using Altus.Core;

namespace Altus.Core.Diagnostics
{
    public static class ObjectLogWriter
    {
        static int __logWritesFailed = 0;
        static int _length = 52428800; // 50 megs
        static int _count = 50;

        static ObjectLogWriter()
        {
            try
            {
                _length = int.Parse(ConfigurationManager.GetAppSetting("ObjectLogLimit"));
            }
            catch { }
            try
            {
                _count = int.Parse(ConfigurationManager.GetAppSetting("ObjectLogCount"));
            }
            catch { }
            Thread writer = new Thread(new ThreadStart(AppendObject));
            writer.Name = "Object Log Writer";
            writer.IsBackground = true;
            writer.Priority = ThreadPriority.BelowNormal;
            writer.Start();
        }

        /// <summary>
        /// Appends the objectText to be written to the file given by Path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="objectText"></param>
        public static bool AppendObject(string path, string objectText)
        {
            return AppendObject(path, objectText, null);
        }

        /// <summary>
        /// Appends the given byet array to the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        public static bool AppendObject(string path, byte[] data)
        {
            return AppendObject(path, data, null);
        }

        /// <summary>
        /// Appends the serialized object to the file given by Path.
        /// The object to be attributed with appropriate Extra Types
        /// to support XML Serialization.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        public static bool AppendObject(string path, object obj, params Type[] extraTypes)
        {
            return AppendObject(path, obj, null, extraTypes);
        }

        /// <summary>
        /// Appends the serialized object to the file given by Path.
        /// The object to be attributed with appropriate Extra Types
        /// to support XML Serialization.  Also allows consumer to provide
        /// a header object to be written in the event that a new storage file is 
        /// create, prior to writing the object data.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        /// <param name="header"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static bool AppendObject(string path, object obj, object header, params Type[] extraTypes)
        {
            try
            {
                lock (_requests)
                {
                    _requests.Enqueue(new AppendRequest() { Path = path, Object = obj, ExtraTypes = extraTypes, HeaderObject = header });
                    _evt.Increment();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        private class AppendRequest
        {
            public AppendRequest() { }
            public string Path { get; set; }
            public object Object { get; set; }
            public Type[] ExtraTypes { get; set; }
            public object HeaderObject { get; set; }
        }

        static Queue<AppendRequest> _requests = new Queue<AppendRequest>();
        static CounterEvent _evt = new CounterEvent();

        private static void AppendObject()
        {
            while (true)
            {
                _evt.WaitOne();
                AppendRequest req = null;
                lock (_requests)
                {
                    req = _requests.Dequeue();
                    _evt.Decrement();
                }

                if (req == null) continue; // shouldn't happen
                string path = req.Path;
                object obj = req.Object;
                Type[] extraTypes = req.ExtraTypes;

                FileStream fs = null;
                bool isNew = false;

                Mutex mutex = null;
                try
                {
                    mutex = new Mutex(true, path.Replace("\\", ""), out isNew);
                    if (!isNew)
                    {
                        // block other callers until we are done writing to this file
                        mutex.WaitOne();
                    }

                    if (System.IO.File.Exists(path))
                    {
                        fs = new FileStream(path,
                            FileMode.Open,
                            FileAccess.ReadWrite,
                            FileShare.Read);
                        fs.Seek(0, SeekOrigin.End);
                    }
                    else
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(path)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                        }
                        fs = new FileStream(path,
                            FileMode.CreateNew,
                            FileAccess.ReadWrite,
                            FileShare.Read);

                        if (req.HeaderObject != null)
                        {
                            WriteObject(req.HeaderObject, fs, req.ExtraTypes);
                        }
                    }

                    fs.Position = fs.Length;

                    WriteObject(obj, fs, req.ExtraTypes);

                    

                    // do length cut off check
                    if (fs.Length >= _length)
                    {
                        using (FileStream fsCompress = new FileStream(path
                            + "."
                            + CurrentTime.Now.ToShortDateString().Replace(@"/", ".")
                            + "."
                            + CurrentTime.Now.ToShortTimeString().Replace(":", ".").Replace(" ", ".")
                            + ".zip",
                            FileMode.Create,
                            FileAccess.ReadWrite,
                            FileShare.Read))
                        {
                            fs.Position = 0;
                            CompressionHelper.Compress(fs, fsCompress, CompressionType.Zip, 9, Path.GetFileNameWithoutExtension(path));
                            fs.Close();
                            fsCompress.Close();
                            fs.Dispose();
                            fsCompress.Dispose();
                            fs = null;

                            if (System.IO.File.Exists(path))
                                System.IO.File.Delete(path);

                            // do log count check
                            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
                            int count = 0;
                            DateTime oldestDate = CurrentTime.Now;
                            string oldestFile = path;

                            // check all the files
                            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path), "*.zip"))
                            {
                                string checkFile = Path.GetFileNameWithoutExtension(file).ToLower();


                                if (checkFile.IndexOf(fileName) >= 0)
                                {
                                    DateTime checkDate = System.IO.File.GetLastWriteTime(file);
                                    if (checkDate < oldestDate)
                                    {
                                        oldestDate = checkDate;
                                        oldestFile = file;
                                    }

                                    count++;
                                }
                            }

                            // delete the oldest file
                            if (count > _count && System.IO.File.Exists(oldestFile))
                                System.IO.File.Delete(oldestFile);
                        }
                    }
                }
                catch (ThreadAbortException) { throw; }
                catch (Exception error)
                {
                    if (Interlocked.Increment(ref __logWritesFailed) == 1)
                        Logger.LogError(error);
                }
                finally
                {
                    // allow other callers to write to the file
                    if (fs != null)
                    {
                        fs.Close();
                    }
                    if (mutex != null)
                    {
                        mutex.ReleaseMutex();
                        mutex = null;
                    }
                }
            }
        }

        private static void WriteObject(object obj, FileStream fs, params Type[] extraTypes)
        {
            if (obj.GetType() == typeof(string))
            {
                // write the text directly to the file stream
                obj = ASCIIEncoding.ASCII.GetBytes((string)obj);
            }
            if (obj.GetType() == typeof(byte[]))
            {
                // write the binary data as-is to the log stream
                StreamHelper.Copy((byte[])obj, fs);
            }
            else
            {
                // encode to XML and write to the file stream
                SerializationHelper.ToXml(obj, fs, extraTypes);
            }
            fs.Write(ASCIIEncoding.ASCII.GetBytes(Environment.NewLine), 0, 2);
            fs.Flush();
        }
    }
}
