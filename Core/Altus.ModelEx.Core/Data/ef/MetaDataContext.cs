using System;
using System.Linq;
using System.Text;
using System.Data.Objects;
using System.Configuration;
using System.Data.EntityClient;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Principal;

using moby.common;
using moby.common.topology;
using moby.realtime.config;
using moby.common.presentation.viewmodels;
using moby.common.graphing;
using moby.common.security;
using moby.common.dynamic;
using System.Net;
using System.Net.Sockets;

namespace moby.common.data.ef
{
    public partial class Entities : IMetaDataContext
    {
        public Entities(IDbConnection connection) : base(GetConnectionString(connection)) 
        { 
            
        }
        public System.Data.Common.DbConnection CurrentConnection
        {
            get;
            private set;
        }
        private static string GetConnectionString(IDbConnection connection)
        {
            EntityConnectionStringBuilder entity = new EntityConnectionStringBuilder();
            entity.Metadata = "res://*/";
            entity.Provider = @"System.Data.SQLite";
            entity.ProviderConnectionString = connection.ConnectionString;
            return entity.ToString();
        }

        public void ExecuteDbScript(string scriptName, params DbParam[] dbParams)
        {
            throw new NotImplementedException();
        }
        public void ExecuteDbScript(string scriptName, DbState state, params DbParam[] dbParams)
        {
            throw new NotImplementedException();
        }
    }

    public static class MetaDataContextEx
    {
        static GraphNode<moby.common.topology.Node> _nodeGraph;
        
        public static int GetNodeCount(this IMetaDataContext ctx)
        {
            Entities myEF = (Entities)ctx;
            {
                return myEF.Nodes.Count();
            }
        }
        private static void BuildNodeGraph(this IMetaDataContext ctx)
        {
            Entities myEF = (Entities)ctx;
            {
                NodeAddress thisAddress = NodeIdentity.NodeAddress();
                var results =
                   (from NT in myEF.NetworkTopologies
                    join NTT in myEF.NetworkTopologyTypes on NT.P2PTypeId equals NTT.Id
                    join Source in myEF.Networks on NT.NetworkSourceId equals Source.Id
                    join NYSource in myEF.NetworkTypes on Source.TypeId equals NYSource.Id
                    join SourceNode in myEF.Nodes on Source.Id equals SourceNode.NetworkId
                    join SourceIdentity in myEF.Identities on SourceNode.IdentityId equals SourceIdentity.Id
                    join SourceOrg in myEF.Organizations on Source.OrganizationId equals SourceOrg.Id
                    join SourceApps in myEF.PlatformOrganizations on SourceOrg.Id equals SourceApps.OrganizationId
                    join SourceApp in myEF.Platforms on SourceApps.PlatformId equals SourceApp.Id
                    join Dest in myEF.Networks on NT.NetworkDestinationId equals Dest.Id
                    join NYDest in myEF.NetworkTypes on Dest.TypeId equals NYDest.Id
                    join DestNode in myEF.Nodes on Dest.Id equals DestNode.NetworkId
                    join DestIdentity in myEF.Identities on DestNode.IdentityId equals DestIdentity.Id
                    join DestOrg in myEF.Organizations on Dest.OrganizationId equals DestOrg.Id
                    join DestApps in myEF.PlatformOrganizations on DestOrg.Id equals DestApps.OrganizationId
                    join DestApp in myEF.Platforms on DestApps.PlatformId equals DestApp.Id
                    //join NB in myEF.NetworkBridges on new { LocalNodeId = SourceNode.IdentityId, RemoteNodeId = DestNode.IdentityId } equals new { NB.LocalNodeId, NB.RemoteNodeId } into nbgroup
                    from NB in myEF.NetworkBridges.Where(NB => NB.LocalNodeId == SourceNode.IdentityId && NB.RemoteNodeId == DestNode.IdentityId).DefaultIfEmpty()
                    where SourceApp.Name == thisAddress.Platform
                        && SourceOrg.Name == thisAddress.Organization
                        && DestApp.Name == thisAddress.Platform
                        && DestOrg.Name == thisAddress.Organization
                    select new
                    {
                        SourceNetId = Source.Id,
                        SourceNet = Source.Name,
                        SourceNetType = NYSource.Name,
                        DestNetId = Dest.Id,
                        DestNet = Dest.Name,
                        DestNetType = NYDest.Name,
                        P2PType = NTT.P2PType,
                        SourceNodeId = SourceIdentity.Id,
                        SourceNodeName = SourceIdentity.Name,
                        SourceOrgId = SourceOrg.Id,
                        SourceOrgName = SourceOrg.Name,
                        SourceAppId = SourceApp.Id,
                        SourceAppName = SourceApp.Name,
                        DestNodeId = DestIdentity.Id,
                        DestNodeName = DestIdentity.Name,
                        DestOrgId = DestOrg.Id,
                        DestOrgName = DestOrg.Name,
                        DestAppId = DestApp.Id,
                        DestAppName = DestApp.Name
                    })
                    .Union(

                   (from SourceNode in myEF.Nodes
                    join SourceIdentity in myEF.Identities on SourceNode.IdentityId equals SourceIdentity.Id
                    join Source in myEF.Networks on SourceNode.NetworkId equals Source.Id
                    join Dest in myEF.Networks on Source.Id equals Dest.Id
                    join SourceOrg in myEF.Organizations on Source.OrganizationId equals SourceOrg.Id
                    join SourceApps in myEF.PlatformOrganizations on SourceOrg.Id equals SourceApps.OrganizationId
                    join SourceApp in myEF.Platforms on SourceApps.PlatformId equals SourceApp.Id
                    join NYSource in myEF.NetworkTypes on Source.TypeId equals NYSource.Id
                    join DestNode in myEF.Nodes on Source.Id equals DestNode.NetworkId
                    join DestIdentity in myEF.Identities on DestNode.IdentityId equals DestIdentity.Id
                    join DestOrg in myEF.Organizations on Dest.OrganizationId equals DestOrg.Id
                    join DestApps in myEF.PlatformOrganizations on DestOrg.Id equals DestApps.OrganizationId
                    join DestApp in myEF.Platforms on DestApps.PlatformId equals DestApp.Id
                    join NTT in myEF.NetworkTopologyTypes on "P2P" equals NTT.P2PType
                    where
                        DestIdentity.Id != SourceIdentity.Id
                        && SourceApp.Name == thisAddress.Platform
                        && SourceOrg.Name == thisAddress.Organization
                        && DestApp.Name == thisAddress.Platform
                        && DestOrg.Name == thisAddress.Organization
                    select new
                    {
                        SourceNetId = Source.Id,
                        SourceNet = Source.Name,
                        SourceNetType = NYSource.Name,
                        DestNetId = Dest.Id,
                        DestNet = Dest.Name,
                        DestNetType = NYSource.Name,
                        P2PType = NTT.P2PType,
                        SourceNodeId = SourceIdentity.Id,
                        SourceNodeName = SourceIdentity.Name,
                        SourceOrgId = SourceOrg.Id,
                        SourceOrgName = SourceOrg.Name,
                        SourceAppId = SourceApp.Id,
                        SourceAppName = SourceApp.Name,
                        DestNodeId = DestIdentity.Id,
                        DestNodeName = DestIdentity.Name,
                        DestOrgId = DestOrg.Id,
                        DestOrgName = DestOrg.Name,
                        DestAppId = DestApp.Id,
                        DestAppName = DestApp.Name
                    }));

                foreach (var r in results)
                {
                    moby.common.topology.Node sourceNode = new moby.common.topology.Node()
                    {
                        Id = (int)(long)r.SourceNodeId,
                        Name = r.SourceNodeName,
                        NetworkId = (int)(long)r.SourceNetId
                    };

                    moby.common.topology.Platform sourceApp = new moby.common.topology.Platform()
                    {
                        Id = (int)(long)r.SourceAppId,
                        Name = r.SourceAppName
                    };

                    moby.common.topology.Organization sourceOrg = new moby.common.topology.Organization()
                    {
                        Id = (int)(long)r.SourceOrgId,
                        Name = r.SourceOrgName,
                        PlatformId = sourceApp.Id,
                        Platform = sourceApp
                    };

                    moby.common.topology.Network sourceNetwork = new moby.common.topology.Network()
                    {
                        Id = (int)(long)r.SourceNetId,
                        Name = r.SourceNet,
                        NetworkType = (moby.common.topology.NetworkType)Enum.Parse(typeof(NetworkType), r.SourceNetType),
                        OrganizationId = sourceOrg.Id,
                        Organization = sourceOrg
                    };

                    sourceNode.Network = sourceNetwork;

                    moby.common.topology.Node destNode = new moby.common.topology.Node()
                    {
                        Id = (int)(long)r.DestNodeId,
                        Name = r.DestNodeName.ToString(),
                        NetworkId = (int)(long)r.DestNetId
                    };

                    moby.common.topology.Platform destApp = new moby.common.topology.Platform()
                    {
                        Id = (int)(long)r.DestAppId,
                        Name = r.DestAppName.ToString()
                    };

                    moby.common.topology.Organization destOrg = new moby.common.topology.Organization()
                    {
                        Id = (int)(long)r.DestOrgId,
                        Name = r.DestOrgName.ToString(),
                        PlatformId = destApp.Id,
                        Platform = destApp
                    };

                    moby.common.topology.Network destNetwork = new moby.common.topology.Network()
                    {
                        Id = (int)(long)r.DestNetId,
                        Name = r.DestNet.ToString(),
                        NetworkType = (moby.common.topology.NetworkType)Enum.Parse(typeof(NetworkType), r.DestNetType.ToString()),
                        OrganizationId = destOrg.Id,
                        Organization = destOrg
                    };

                    destNode.Network = destNetwork;
                    GraphNode<moby.common.topology.Node> graph = new GraphNode<moby.common.topology.Node>(
                    new moby.common.topology.Node()
                    {
                        Name = "root",
                        Id = 0,
                        NetworkId = 0,
                        Network = new moby.common.topology.Network()
                        {
                            Id = 0,
                            Name = "moby",
                            NetworkType =
                            moby.common.topology.NetworkType.Private,
                            Organization = new moby.common.topology.Organization()
                            {
                                Id = 0,
                                Platform = new moby.common.topology.Platform()
                                {
                                    Id = 0,
                                    Name = "moby"
                                },
                                Name = "moby"
                            }
                        }
                    });

                    GraphNode<moby.common.topology.Node> foundSource = graph.Find(delegate(moby.common.topology.Node node)
                    {
                        return node.NetworkId.Equals(sourceNode.NetworkId)
                            && node.Name.Equals(sourceNode.Name, StringComparison.InvariantCultureIgnoreCase);
                    });

                    if (foundSource == null)
                    {
                        foundSource = new GraphNode<moby.common.topology.Node>(sourceNode);
                        graph.Add(foundSource);
                    }

                    GraphNode<moby.common.topology.Node> foundDest = graph.Find(delegate(moby.common.topology.Node node)
                    {
                        return node.NetworkId.Equals(destNode.NetworkId)
                            && node.Name.Equals(destNode.Name, StringComparison.InvariantCultureIgnoreCase);
                    });

                    if (foundDest == null)
                    {
                        foundDest = new GraphNode<moby.common.topology.Node>(destNode);
                    }

                    foundSource.Add(foundDest);
                    _nodeGraph.Add(graph);
                }
            }
        }
        public static bool TryGetNodeEndPoint(this IMetaDataContext ctx, string networkQualifiedNodeName, string protocol, out IPEndPoint endPoint)
        {
            endPoint = null;
            try
            {
                endPoint = GetNodeEndPoint(ctx, networkQualifiedNodeName, protocol);
            }
            catch (ProtocolViolationException) { }
            catch (SocketException) { }

            return endPoint != null;
        }
        public static IPEndPoint GetNodeEndPoint(this IMetaDataContext ctx, string networkQualifiedNodeName, string protocol)
        {

            if (networkQualifiedNodeName.Equals("*")) return new IPEndPoint(IPAddress.Any, 0);
            if (_nodeGraph == null)
                BuildNodeGraph(ctx);

            string thisFQNN = NodeIdentity.NodeAddress();

            NodeAddress thisAddress = thisFQNN;
            NodeAddress thatAddress = networkQualifiedNodeName;


            GraphNode<moby.common.topology.Node> thisNode = _nodeGraph.Find(delegate(moby.common.topology.Node node)
            {
                return node.Network.Organization.Platform.Name.Equals(thisAddress.Platform, StringComparison.InvariantCultureIgnoreCase)
                    && node.Network.Organization.Name.Equals(thisAddress.Organization, StringComparison.InvariantCultureIgnoreCase)
                    && node.Network.Name.Equals(thisAddress.Network, StringComparison.InvariantCultureIgnoreCase)
                    && node.Name.Equals(thisAddress.Node, StringComparison.InvariantCultureIgnoreCase);
            });

            if (thisNode == null) throw (new InvalidOperationException("The local node name could not be resolved"));

            Stack<GraphNode<moby.common.topology.Node>> travsersal = thisNode.Traverse(delegate(moby.common.topology.Node node)
            {
                return node.Network.Organization.Platform.Name.Equals(thatAddress.Platform, StringComparison.InvariantCultureIgnoreCase)
                    && node.Network.Organization.Name.Equals(thatAddress.Organization, StringComparison.InvariantCultureIgnoreCase)
                    && node.Network.Name.Equals(thatAddress.Network, StringComparison.InvariantCultureIgnoreCase)
                    && node.Name.Equals(thatAddress.Node, StringComparison.InvariantCultureIgnoreCase);
            }).FirstOrDefault();

            if (travsersal == null) throw (new InvalidOperationException("The remote node name could not be resolved"));

            while (travsersal.Count > 2)
                travsersal.Pop();  // these nodes come after our next immediate node
            GraphNode<moby.common.topology.Node> nextNode = travsersal.Pop();

            int targetNodeId = nextNode.Value.Id;

            DbState state = new DbState()
            {
                Callback = new DbCallback(delegate(DbState s)
                {
                    if (s.Reader.Read())
                    {
                        string ip = s.Reader["HostNameOrIP"].ToString();
                        int port = (int)(long)s.Reader["Port"];
                        IPAddress[] addresses = GetHostAddresses(ip);
                        if (addresses.Length == 0) throw (new ProtocolViolationException("The protocol is not supported for the target node"));

                        IPAddress address = addresses[0];
                        foreach (IPAddress ipad in addresses)
                        {
                            if (ipad.ToString().Equals(ip))
                            {
                                address = ipad;
                                break;
                            }
                        }

                        IPEndPoint ipep = new IPEndPoint(address, port);
                        s.StateObject = ipep;
                    }
                    else
                        throw (new ProtocolViolationException("The protocol is not supported for the target node"));
                })
            };
            ctx.ExecuteDbScript("GetNodeEndPoint", state, new DbParam("Id", targetNodeId), new DbParam("Protocol", protocol));
            return state.StateObject as IPEndPoint;
        }
        public static ulong GetNodeId(this IMetaDataContext ctx, string nodeAddress)
        {
            NodeAddress address = nodeAddress;
            Entities myEF = (Entities)ctx;
            {
                return
                    (ulong)(from N in myEF.Nodes
                            join I in myEF.Identities on N.IdentityId equals I.Id
                            join NT in myEF.Networks on N.NetworkId equals NT.Id
                            join O in myEF.Organizations on NT.OrganizationId equals O.Id
                            join PO in myEF.PlatformOrganizations on O.Id equals PO.OrganizationId
                            join A in myEF.Platforms on PO.PlatformId equals A.Id
                            where I.Name == address.Node
                            && NT.Name == address.Network
                            && O.Name == address.Organization
                            && A.Name == address.Platform
                            select I).FirstOrDefault<Identity>().Id;
            }
        }
        public static IEnumerable<object> GetNodeNetworkIdentities(this IMetaDataContext ctx)
        {
            Entities myEF = (Entities)ctx;
            {
                return (from node in myEF.Nodes
                        join identity in myEF.Identities on node.IdentityId equals identity.Id into identity
                        join network in myEF.Networks on node.NetworkId equals network.Id into network
                        select new { Node = node, Identity = identity, Network = network }).ToList();
            }
        }

        public static string GetOrgId(this IMetaDataContext ctx)
        {
            NodeAddress address = NodeIdentity.NodeAddress();
            Entities myEF = (Entities)ctx;
            {
                return
                    ((from O in myEF.Organizations
                      where O.Name == address.Organization
                      select O).FirstOrDefault<Organization>().Id.ToString());
            }
        }
        public static string OrgIdentityExists(this IMetaDataContext ctx, string aIdentityName, string aOrgId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from I in myEF.Identities
                     join OI in myEF.OrganizationIdentities on I.Id equals OI.IdentityId
                     where I.Name == aIdentityName
                     && OI.OrganizationId == Convert.ToInt32(aOrgId)
                     select I).FirstOrDefault<Identity>().Name;
            }
        }
        public static string GetOrgIdentityPassword(this IMetaDataContext ctx, string aIdentityName, string aOrgId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from I in myEF.Identities
                     join OI in myEF.OrganizationIdentities on I.Id equals OI.IdentityId
                     where I.Name == aIdentityName
                     && OI.OrganizationId == Convert.ToInt32(aOrgId)
                     select I).FirstOrDefault<Identity>().PasswordEncrypted;
            }
        }
        public static string GetOrgIdentitySecretKey(this IMetaDataContext ctx, string aIdentityName, string aOrgId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from I in myEF.Identities
                     join OI in myEF.OrganizationIdentities on I.Id equals OI.IdentityId
                     where I.Name == aIdentityName
                     && OI.OrganizationId == Convert.ToInt32(aOrgId)
                     select I).FirstOrDefault<Identity>().SecretKey;
            }
        }
        
        public static string GetPlatformId(this IMetaDataContext ctx)
        {
            NodeAddress address = NodeIdentity.NodeAddress();
            Entities myEF = (Entities)ctx;
            {
                return
                    (from P in myEF.Platforms
                     where P.Name == address.Platform
                     select P).FirstOrDefault<Platform>().Id.ToString();
            }
        }
        public static string GetPlatformRole(this IMetaDataContext ctx, string aRoleName, int aPlatformId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from R in myEF.Roles
                     join PR in myEF.PlatformRoles on R.Id equals PR.RoleId
                     where R.Name == aRoleName
                     && PR.PlatformId == aPlatformId
                     select R).FirstOrDefault<Role>().Name;
            }
        }
        public static List<string> GetPlatformRoles(this IMetaDataContext ctx, int aPlatformId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from R in myEF.Roles
                     join PR in myEF.PlatformRoles on R.Id equals PR.RoleId
                     where PR.PlatformId == aPlatformId
                     select R) as List<string>;
            }
        }
        
        public static string GetIdentityId(this IMetaDataContext ctx, string aIdentityName, int aOrganizationId)
        {
            Entities myEF = (Entities)ctx;
            {
                ObjectSet<Identity> identities = myEF.Identities;
                ObjectSet<OrganizationIdentity> orgIdentities = myEF.OrganizationIdentities;

                return
                    (from I in identities
                     join OI in orgIdentities
                     on I.Id equals OI.IdentityId
                     where I.Name == aIdentityName
                     && OI.OrganizationId == aOrganizationId
                     select I).FirstOrDefault<Identity>().Id.ToString();
            }
        }
        public static string IdentityHasRoll(this IMetaDataContext ctx, string aRoleName, int aOrganizationId, int aIdentityId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from R in myEF.Roles
                     join IR in myEF.IdentityRoles on R.Id equals IR.RoleId
                     join OI in myEF.OrganizationIdentities on IR.IdentityId equals OI.IdentityId
                     where OI.OrganizationId == aOrganizationId
                     && R.Name == aRoleName
                     && OI.IdentityId == aIdentityId
                     select R).FirstOrDefault<Role>().Name;
            }
        }
        public static bool ContainsIdentity(this IMetaDataContext ctx, string aIdentityName)
        {
            return ctx.OrgIdentityExists(aIdentityName, ctx.GetOrgId()) != null;
        }

        public static string GetSecretKey(this IMetaDataContext ctx, string nodeAddress)
        {
            NodeAddress address = nodeAddress;
            Entities myEF = (Entities)ctx;
            {
                return
                   (from I in myEF.Identities
                    join N in myEF.Nodes on I.Id equals N.IdentityId
                    join NT in myEF.Networks on N.NetworkId equals NT.Id
                    join O in myEF.Organizations on NT.OrganizationId equals O.Id
                    join PO in myEF.PlatformOrganizations on O.Id equals PO.OrganizationId
                    join A in myEF.Platforms on PO.PlatformId equals A.Id
                    where I.Name == address.Node
                    && NT.Name == address.Network
                    && O.Name == address.Organization
                    && A.Name == address.Platform
                    select I).FirstOrDefault<Identity>().SecretKey;
            }
        }
        public static string GetSecretKey(this IMetaDataContext ctx, ulong nodeId)
        {
            Entities myEF = (Entities)ctx;
            {
                return
                    (from I in myEF.Identities
                     where I.Id == (long)nodeId
                     select I).FirstOrDefault<Identity>().SecretKey;
            }
        }
        public static string GetHashedPassword(this IMetaDataContext ctx, string aIdentityName)
        {
            if (ctx.ContainsIdentity(aIdentityName))
            {
                return ctx.GetOrgIdentityPassword(aIdentityName, ctx.GetOrgId());
            }
            return "";
        }
        public static string GetSalt(this IMetaDataContext ctx, string aIdentityName)
        {
            if (ctx.ContainsIdentity(aIdentityName))
            {
                return DataContext.Meta.GetOrgIdentitySecretKey(aIdentityName, DataContext.Meta.GetOrgId());
            }
            return "";
        }
        public static bool IsValidNameAndPassword(this IMetaDataContext ctx, string aIdentityName, string aPasswordRaw)
        {
            string storedHashedPW = ctx.GetHashedPassword(aIdentityName);
            string rawSalted = ctx.GetSalt(aIdentityName) + aPasswordRaw.Trim();
            byte[] saltedPwBytes = Encoding.Unicode.GetBytes(rawSalted);
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            return (Convert.ToBase64String(sha1.ComputeHash(saltedPwBytes)) == storedHashedPW);
        }
        public static IPrincipal Authenticate(this IMetaDataContext ctx, string aUserName, string aPassword)
        {
            string[] Organizations = new string[0]; // replace with call to ctx.GetOrganizations
            string[] Roles = ctx.GetPlatformRoles(int.Parse(ctx.GetPlatformId())).ToArray();
            security.Identity myIdentity = new security.Identity(aUserName, Organizations);
            if (ctx.IsValidNameAndPassword(aUserName, aPassword))
            {
                myIdentity.IsAuthenticated = true;
                IPrincipal myPrincipal = new Principal(myIdentity, Roles);
                return myPrincipal;
            }

            return null;
        }

        public static bool GetFieldIds(this IMetaDataContext ctx, ref Field field)
        {
            /*
            DbState state = new DbState()
            {
                Callback = new DbCallback(delegate(DbState s)
                {
                    if (s.Reader.Read())
                    {
                        ulong composite = (ulong)((ushort)(long)s.Reader["FieldId"]
                                        + ((ushort)(long)s.Reader["ObjectId"] << 2)
                                        + ((uint)(long)s.Reader["ObjectSpaceId"] << 4));
                        s.StateObject = composite;
                    }
                }),
                StateObject = 0
            };

            ctx.ExecuteDbScript("GetFieldIds", state,
                new DbParam("FieldName", field.Name),
                new DbParam("ObjectName", field.Object),
                new DbParam("ObjectSpace", field.ObjectSpace));
            field.CompositeId = (ulong)state.StateObject;
            return (ulong)state.StateObject > 0;
             * 
             * */

            Entities myEF = (Entities)ctx;
            {                
                  
            }
            return false;
        }
        private static void GetFieldCalculators(this IMetaDataContext ctx, FieldConfig config, ObjectConfiguration objectCfg, EventHandlerConfig evtCfg)
        {
            throw new NotImplementedException();
        }
        public static IEnumerable<DynamicProperty<DynamicField>> GetFieldProperties(this IMetaDataContext ctx, DynamicField field)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<ObjectConfiguration> GetLocalObjectConfigs(this IMetaDataContext ctx)
        {
            /*
            NodeAddress address = NodeIdentity.NodeAddress();
            DbState state = new DbState()
            {
                Callback = new DbCallback(delegate(DbState s)
                {
                    List<ObjectConfiguration> configs = new List<ObjectConfiguration>();
                    while (s.Reader.Read())
                    {
                        ObjectConfiguration config = new ObjectConfiguration()
                        {
                            Id = (ushort)(long)s.Reader["Id"],
                            ObjectSpaceId = (uint)(long)s.Reader["ObjectSpaceId"],
                            ObjectSpace = s.Reader["ObjectSpace"].ToString(),
                            Name = s.Reader["Name"].ToString(),
                            DefaultCompressionStrategyType = s.Reader["DefaultCompressionStrategyType"].ToString()
                        };
                        configs.Add(config);
                    }

                    foreach (ObjectConfiguration config in configs)
                    {
                        GetObjectFieldConfigs(ctx, config);
                    }

                    s.StateObject = configs;
                })
            };
            ctx.ExecuteDbScript("GetLocalHistoryObjects", state,
                new DbParam("App", address.Platform),
                new DbParam("Org", address.Organization),
                new DbParam("Net", address.Network), new DbParam("Node", address.Node));
            return state.StateObject as IEnumerable<ObjectConfiguration>;
             * */
            throw new NotImplementedException();
        }
        private static void GetObjectFieldConfigs(this IMetaDataContext ctx, ObjectConfiguration config)
        {
            throw new NotImplementedException();
        }
        
        public static bool CheckIsMulticast(this IMetaDataContext ctx, IPEndPoint endPoint)
        {
            /*DbState state = new DbState()
            {
                Callback = new DbCallback(delegate(DbState s)
                {
                    bool ret = false;
                    if (s.Reader.Read())
                    {
                        ret = (long)s.Reader["Exists"] > 0;
                    }
                    s.StateObject = ret;
                })
            };

            ctx.ExecuteDbScript("CheckMulticast", state,
                new DbParam("IP", endPoint.Address.ToString()),
                new DbParam("Port", endPoint.Port));
            return (bool)state.StateObject;
             * */
            throw new NotImplementedException();
        }
        private static IPAddress[] GetHostAddresses(string ip)
        {
            IPAddress[] addresses = Context.Cache[ip] as IPAddress[];
            if (addresses == null)
            {
                addresses = Dns.GetHostAddresses(ip);
                Context.Cache.Add(ip, addresses, DateTime.MinValue, TimeSpan.FromHours(12), new string[0]);
            }
            return addresses;
        }

        public static IEnumerable<Topic> GetTopics(this IMetaDataContext ctx)
        {            
            NodeAddress address = NodeIdentity.NodeAddress();
            Entities myEF = (Entities)ctx;
            {
                return
                   (from T in myEF.Topics
                    join O in myEF.Organizations on T.OrganizationId equals O.Id
                    where O.Name == address.Organization                    
                    select T).ToList<Topic>();
            }
        }
        public static Topic GetTopic(this IMetaDataContext ctx, string name)
        {
            NodeAddress address = NodeIdentity.NodeAddress();
            Entities myEF = (Entities)ctx;
            {
                return
                    (from T in myEF.Topics
                     join O in myEF.Organizations on T.OrganizationId equals O.Id
                     join P in myEF.Platforms on T.PlatformId equals P.Id
                     where O.Name == address.Organization
                     && T.Name == name
                     && P.Name == address.Platform
                     select T).FirstOrDefault<Topic>();
            }
        }

        public static string GetContentType(this IMetaDataContext ctx, string extension)
        {
            string myStateResult = string.Empty;
            Entities myEF = (Entities)ctx;
            {
                return
                    ((from C in myEF.ContentTypes
                      where C.Extension == extension
                      select C).FirstOrDefault<ContentType>().Id.ToString());
            }
        }
    }
}
