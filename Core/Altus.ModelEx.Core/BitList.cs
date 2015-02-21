using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core
{
    public class BitList
    {
        public BitList(params bool[] bits)
        {
            Bits = bits;
        }

        public BitList(params int[] bits)
        {
            bool[] newBits = new bool[bits.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                newBits[i] = Bit(bits[i]);
            }
            Bits = newBits;
        }

        public static bool Bit(int p)
        {
            return p != 0;
        }

        private bool[] _bits = null;
        public bool[] Bits { get { return _bits; } set { _bits = value; CalculateValue(); } }

        private void CalculateValue()
        {
            Value = 0;
            for (int i = Bits.Length - 1; i >= 0; i--)
            {
                Value |= (Int(Bits[i]) << ((Bits.Length - 1) - i));
            }
        }

        public int Value { get; private set; }

        public static implicit operator int(BitList bits)
        {
            return bits.Value;
        }

        public static int Int(bool bit)
        {
            return bit ? 1 : 0;
        }
    }
}
