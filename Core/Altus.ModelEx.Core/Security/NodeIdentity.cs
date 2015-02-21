using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Altus.Core.Component;
using Altus.Core.Data;
using Altus.Core.Licensing;
using Altus.Core.Security;
using Altus.Core.Topology;


namespace Altus.Core.Security
{
    public class NodeIdentity : LicensedComponent
    {
        static NodeIdentity()
        {
            if (App.Instance != null)
                App.Instance.Shell.Add(new NodeIdentity());
        }

        private NodeIdentity() { }

        static Dictionary<string, byte[]> _keys = new Dictionary<string, byte[]>();
        public static byte[] SecretKey(string sender)
        {
            lock (_keys)
            {
                if (!_keys.ContainsKey(sender))
                {
                    try
                    {
                        _keys.Add(sender, Encoding.ASCII.GetBytes(DataContext.Default.Get<Identity>(new { nodeAddress = sender }).SecretKey));
                    }
                    catch
                    {
                        _keys.Add(sender, new byte[0]);
                    }
                }
            }
            return _keys[sender];
        }

        static ulong _id;
        public static ulong NodeId
        {
            get
            {
                return _id;
            }
        }



        static string _na = string.Empty;
        public static string NodeAddress
        {
            get
            {
                return _na;
            }
        }

        public static bool TryGetNodeId(string address, out ulong nodeId)
        {
            nodeId = 0;
            try
            {
                Identity node = DataContext.Default.Get<Identity>(new { nodeAddress = address });
                if (node == null)
                    return false;
                nodeId = (ulong)node.Id;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string _app;
        public static string Application
        {
            get
            {
                return _app;
            }
        }


        protected override bool OnIsLicensed(object component)
        {
            return true;
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        protected override void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {
            
        }
    }
}
