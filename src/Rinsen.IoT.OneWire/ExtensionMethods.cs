﻿namespace Rinsen.IoT.OneWire
{
    public static class ExtensionMethods
    {
        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        public static bool GetBit(this ushort b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
    }
}
