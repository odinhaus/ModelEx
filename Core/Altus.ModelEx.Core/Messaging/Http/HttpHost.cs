using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.Net;
using Altus.Core.Diagnostics;
using Altus.Core.Messaging;
using System.Collections.Specialized;
using System.Collections;
using Altus.Core.Pipeline;
using Altus.Core.Streams;
using System.IO;
using System.Threading;
using Altus.Core.Serialization;
using Altus.Core.Processing;
using Altus.Core.Messaging.Http;
using Altus.Core.Data;
using Altus.Core.Security;
using System.Diagnostics;

[assembly: Component(
    ComponentType = typeof(HttpHost),
    Name = "HttpHost")]

namespace Altus.Core.Messaging.Http
{
    public class HttpHost : InitializableComponent
    {
        HttpListener _listener;
        bool _running = false;

        protected override bool OnInitialize(params string[] args)
        {
            IPEndPoint endPoint;
            string httpVirtual = Context.GetEnvironmentVariable<string>("HttpVirtual", "");

            if (!string.IsNullOrEmpty(httpVirtual)
                && !httpVirtual.EndsWith("/")) httpVirtual += "/";

            if (!DataContext.Default.TryGetNodeEndPoint(NodeIdentity.NodeAddress, "http", out endPoint))
            {
                string[] httpEndPoint = Context.GetEnvironmentVariable("HttpEndPoint", "").Split(':');
                if (httpEndPoint.Length == 1)
                {
                    try
                    {
                        IPAddress add = Dns.GetHostAddresses(httpEndPoint[0])
                            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .First(); ;
                        endPoint = new IPEndPoint(add, 80);
                    }
                    catch(Exception e)
                    {
                        Logger.Log(e);
                    }
                }
                else if (httpEndPoint.Length == 2)
                {
                    try
                    {
                        IPAddress add = Dns.GetHostAddresses(httpEndPoint[0])
                            .Where(a=> a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .First();
                        int port = int.Parse(httpEndPoint[1]);
                        endPoint = new IPEndPoint(add, port);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e);
                    }
                }
            }
            if (endPoint != null)
            { 
                _running = true;
                ServicePointManager.DefaultConnectionLimit = 25;
                bool tried = false;
            retry:
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add("http://" + endPoint.Address.ToString() 
                        + ":" + endPoint.Port.ToString() + "/" + httpVirtual);
                    Logger.LogInfo("Http Listener started for " + endPoint.Address.ToString()
                        + ":" + endPoint.Port.ToString());
                    _listener.Start();
                    _listener.BeginGetContext(new AsyncCallback(GetHttpContextAsync), null);
                }
                catch (HttpListenerException ex)
                {
                    string regCmd = string.Format("netsh http add urlacl url=http://{0}:{1}/{2} user=users", endPoint.Address.ToString(), endPoint.Port, httpVirtual);

                    if (ex.ErrorCode == 5) 
                    {
                        if (tried)
                        {
                            Logger.LogError(new Exception("HTTP Listener could not be created.  Run the following command with administrative privileges to correct the error: "
                                + regCmd));
                        }
                        else
                        {
                            Process process = new Process();
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.FileName = "cmd.exe";
                            startInfo.Arguments = "/C " + regCmd;
                            startInfo.UseShellExecute = true;
                            startInfo.Verb = "runas";
                            process.StartInfo = startInfo;
                            tried = true;
                            try
                            {
                                process.Start();

                                if (!process.WaitForExit(2000))
                                    process.Kill();
                            }
                            catch (Exception uex) { Logger.LogError(uex); }
                            goto retry;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return true;
        }

        int _ptr = 0;
        protected void GetHttpContextAsync(IAsyncResult result)
        {
            try
            {
                HttpListenerContext ctx = _listener.EndGetContext(result);
                Thread worker = new Thread(new ParameterizedThreadStart(ProcessRequest));
                worker.Name = "HTTP Request Processor";
                worker.IsBackground = true;
                worker.Start(ctx);

                if (_running)
                    _listener.BeginGetContext(new AsyncCallback(GetHttpContextAsync), null);
            }
            catch { }
        }

        protected void ProcessRequest(object state)
        {
            if (!_running) return;

            Context.CurrentContext = Context.GlobalContext;
            HttpListenerContext ctx = state as HttpListenerContext;// null;
            HttpRequestProcessorFactory.CreateProcessor(ctx).ProcessRequest();
        }


        protected override void OnDispose()
        {
            if (_listener != null)
                _listener.Stop();

            _running = false;

            base.OnDispose();
        }
    }

    internal class WorkerState
    {
        public ManualResetEvent Switch { get; set; }
        public Queue<HttpListenerContext> ContextQueue { get; set; }
        public Thread Thread { get; set; }
    }
}
