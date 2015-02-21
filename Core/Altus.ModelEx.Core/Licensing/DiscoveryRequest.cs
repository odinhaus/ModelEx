using Altus.Core.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Licensing
{
    public enum ProcessorArchitecture
    {
        _32Bit,
        _64Bit
    }

    public class DiscoveryRequest
    {
        [BinarySerializable(1)]
        public string ProviderUri { get; set; }
    }
}
