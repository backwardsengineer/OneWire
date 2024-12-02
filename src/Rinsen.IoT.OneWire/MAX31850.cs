using Rinsen.IoT.OneWire.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IoT.OneWire
{
    public class MAX31850 : IOneWireDevice
    {
        public class TemperatureConversionResult
        {
            public bool CRC_OK = false;
            public bool Fault;
            public bool ShortToVdd;
            public bool ShortToGnd;
            public bool OpenCircuit;
            public double ColdJunctionCompensatedThermocoupleTemperature;
            public double InternalColdJunctionTemperature;

            public string TemperatureConversionResultNarrative
            {
                get
                {
                    StringBuilder sb = new StringBuilder();

                    if (!CRC_OK)
                    {
                        sb.Append("CRC Error");
                        return sb.ToString();
                    }

                    if (Fault)
                    {
                        if (ShortToVdd)
                        {
                            sb.Append("Short to Vdd");
                        }

                        if (ShortToGnd)
                        {
                            sb.Append("Short to Gnd");
                        }
                        
                        if (OpenCircuit)
                        {
                            sb.Append("Open circuit");
                        }

                        return sb.ToString();
                    }

                    sb.Append("Internal cold junction temperature : ");
                    sb.Append(InternalColdJunctionTemperature);
                    sb.Append(Environment.NewLine);

                    sb.Append("Cold junction compensated thermocouple temperature : ");
                    sb.Append(ColdJunctionCompensatedThermocoupleTemperature);
                    sb.Append(Environment.NewLine);

                    return sb.ToString();
                }
            }
        }

        public class AddressPinsResult
        {
            public bool CRC_OK = false;
            public bool[] AddressPins = new bool[4];
        }

        private OneWireMaster _oneWireMaster;

        public byte[] OneWireAddress { get; private set; }

        public string OneWireAddressString { get { return BitConverter.ToString(OneWireAddress); } }

        private byte[] scratchpad;

        private bool scrachpadCRC_OK = false;
        public void Initialize(OneWireMaster oneWireMaster, byte[] oneWireAddress)
        {
            _oneWireMaster = oneWireMaster;
            OneWireAddress = oneWireAddress;
        }

        /// <summary>
        /// The address pins that are used to locate the physical device (they aren't address pins for addressing the device)
        /// </summary>
        /// <returns>AddressPinsResult</returns>
        public AddressPinsResult GetAddressPins()
        {
            RetrieveScratchpad();

            AddressPinsResult result = new AddressPinsResult();

            result.CRC_OK = scrachpadCRC_OK;
            result.AddressPins[0] = (scratchpad[Scratchpad.ConfigurationRegister] & 1) != 0;
            result.AddressPins[1] = (scratchpad[Scratchpad.ConfigurationRegister] & 2) != 0;
            result.AddressPins[2] = (scratchpad[Scratchpad.ConfigurationRegister] & 4) != 0;
            result.AddressPins[3] = (scratchpad[Scratchpad.ConfigurationRegister] & 8) != 0;
            return result;
        }

        /// <summary>
        /// CopyBit creates a new ushort based on the bit specified from a scratchpad byte
        /// </summary>
        /// <param name="tmp">Temperature ushort to work on</param>
        /// <param name="scratchpadByteNumber">The origin scratchpad byte location number</param>
        /// <param name="originalBitPosition">The bit position to extract from the scratchpad byte</param>
        /// <param name="newBitPosition">The bit position to place the extracted bit in</param>
        /// <returns></returns>
        private ushort CopyBit(ushort tmp, byte scratchpadByteNumber, byte originalBitPosition, byte newBitPosition)
        {
            ushort temp = tmp;
            if ((scratchpad[scratchpadByteNumber] & (1 << originalBitPosition)) != 0)
            {
                temp = (ushort)(temp | (1 << newBitPosition));

            }
            return temp;
        }

        private double GetColdJunctionCompensatedThermocoupleTemperature()
        {
            ushort msblsb = 0;  // Combined and reordered msb and lsb

            msblsb = CopyBit(msblsb, 0, 2, 0);
            msblsb = CopyBit(msblsb, 0, 3, 1);
            msblsb = CopyBit(msblsb, 0, 4, 2);
            msblsb = CopyBit(msblsb, 0, 5, 3);
            msblsb = CopyBit(msblsb, 0, 6, 4);
            msblsb = CopyBit(msblsb, 0, 7, 5);
            msblsb = CopyBit(msblsb, 1, 0, 6);
            msblsb = CopyBit(msblsb, 1, 1, 7);
            msblsb = CopyBit(msblsb, 1, 2, 8);
            msblsb = CopyBit(msblsb, 1, 3, 9);
            msblsb = CopyBit(msblsb, 1, 4, 10);
            msblsb = CopyBit(msblsb, 1, 5, 11);
            msblsb = CopyBit(msblsb, 1, 6, 12);

            bool sign = scratchpad[1].GetBit(7);
            bool negative = sign;

            if (negative) // Temperature is negative
            {
                msblsb = (ushort)~msblsb;                           // Flip the bits
                msblsb = (ushort)(msblsb & 0b0001111111111111);     // Discard the 3 most significant bits
                msblsb++;                                           // Add one
            }

            double temperature = 0;

            if (msblsb.GetBit(0)) temperature += PowerValues[-2];  // Math,Pow(2,-2)
            if (msblsb.GetBit(1)) temperature += PowerValues[-1];  // Math,Pow(2,-1)
            if (msblsb.GetBit(2)) temperature += PowerValues[0];   // Math,Pow(2,0)
            if (msblsb.GetBit(3)) temperature += PowerValues[1];   // Math,Pow(2,1)
            if (msblsb.GetBit(4)) temperature += PowerValues[2];   // Math,Pow(2,2)
            if (msblsb.GetBit(5)) temperature += PowerValues[3];   // Math,Pow(2,3)
            if (msblsb.GetBit(6)) temperature += PowerValues[4];   // Math,Pow(2,4)
            if (msblsb.GetBit(7)) temperature += PowerValues[5];   // Math,Pow(2,5)
            if (msblsb.GetBit(8)) temperature += PowerValues[6];   // Math,Pow(2,6)
            if (msblsb.GetBit(9)) temperature += PowerValues[7];   // Math,Pow(2,7)
            if (msblsb.GetBit(10)) temperature += PowerValues[8];  // Math,Pow(2,8)
            if (msblsb.GetBit(11)) temperature += PowerValues[9];  // Math,Pow(2,9)
            if (msblsb.GetBit(12)) temperature += PowerValues[10]; // Math,Pow(2,10)

            if (negative) // Temperature is negative
            {
                temperature = temperature * -1;
            }

            return temperature;
        }

        private double GetInternalColdJunctionTemperature()
        {
            ushort msblsb = 0;  // Combined and reordered msb and lsb

            msblsb = CopyBit(msblsb, 2, 4, 0);
            msblsb = CopyBit(msblsb, 2, 5, 1);
            msblsb = CopyBit(msblsb, 2, 6, 2);
            msblsb = CopyBit(msblsb, 2, 7, 3);
            msblsb = CopyBit(msblsb, 3, 0, 4);
            msblsb = CopyBit(msblsb, 3, 1, 5);
            msblsb = CopyBit(msblsb, 3, 2, 6);
            msblsb = CopyBit(msblsb, 3, 3, 7);
            msblsb = CopyBit(msblsb, 3, 4, 8);
            msblsb = CopyBit(msblsb, 3, 5, 9);
            msblsb = CopyBit(msblsb, 3, 6, 10);

            bool sign = scratchpad[3].GetBit(7);
            bool negative = sign;

            if (negative) // Temperature is negative
            {
                msblsb = (ushort)~msblsb;                           // Flip the bits
                msblsb = (ushort)(msblsb & 0b0000011111111111);     // Discard the 5 most significant bits
                msblsb++;                                           // Add one
            }

            double temperature = 0;

            if (msblsb.GetBit(0)) temperature += PowerValues[-4];  // Math,Pow(2,-4)
            if (msblsb.GetBit(1)) temperature += PowerValues[-3];  // Math,Pow(2,-3)
            if (msblsb.GetBit(2)) temperature += PowerValues[-2];  // Math,Pow(2,-2)
            if (msblsb.GetBit(3)) temperature += PowerValues[-1];  // Math,Pow(2,-1)
            if (msblsb.GetBit(4)) temperature += PowerValues[0];   // Math,Pow(2,0)
            if (msblsb.GetBit(5)) temperature += PowerValues[1];   // Math,Pow(2,1)
            if (msblsb.GetBit(6)) temperature += PowerValues[2];   // Math,Pow(2,2)
            if (msblsb.GetBit(7)) temperature += PowerValues[3];   // Math,Pow(2,3)
            if (msblsb.GetBit(8)) temperature += PowerValues[4];   // Math,Pow(2,4)
            if (msblsb.GetBit(9)) temperature += PowerValues[5];   // Math,Pow(2,5)
            if (msblsb.GetBit(10)) temperature += PowerValues[6];  // Math,Pow(2,6)

            if (negative) // Temperature is negative
            {
                temperature = temperature * -1;
            }

            return temperature;
        }

        public TemperatureConversionResult GetTemperature()
        {
            RetrieveTemperatureScratchpad();

            TemperatureConversionResult result = new TemperatureConversionResult();
            result.CRC_OK = scrachpadCRC_OK;
            result.Fault = scratchpad[0].GetBit(0);
            result.OpenCircuit = scratchpad[2].GetBit(0);
            result.ShortToGnd = scratchpad[2].GetBit(1);
            result.ShortToVdd = scratchpad[2].GetBit(2);

            result.ColdJunctionCompensatedThermocoupleTemperature = GetColdJunctionCompensatedThermocoupleTemperature();
            result.InternalColdJunctionTemperature = GetInternalColdJunctionTemperature();

            return result;
        }

        protected void RetrieveScratchpad()
        {
            scratchpad = GetScratchpad();
            scrachpadCRC_OK = CalculateCRC();
        }

        protected byte[] GetScratchpad()
        {
            ResetOneWireAndMatchDeviceRomAddress();
            return ReadScratchpad();
        }

        protected void RetrieveTemperatureScratchpad()
        {
            scratchpad = GetTemperatureScratchpad();
            scrachpadCRC_OK = CalculateCRC();
        }

        protected byte[] GetTemperatureScratchpad()
        {
            ResetOneWireAndMatchDeviceRomAddress();
            StartTemperatureConversion();

            ResetOneWireAndMatchDeviceRomAddress();

            return ReadScratchpad();
        }

        void StartTemperatureConversion()
        {
            _oneWireMaster.WriteByte(FunctionCommand.CONVERT_T);

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        }

        byte[] ReadScratchpad()
        {
            _oneWireMaster.WriteByte(FunctionCommand.READ_SCRATCHPAD);

            var scratchpadData = new byte[9];

            for (int i = 0; i < scratchpadData.Length; i++)
            {
                scratchpadData[i] = _oneWireMaster.ReadByte();
            }

            return scratchpadData;
        }

        void ResetOneWireAndMatchDeviceRomAddress()
        {
            _oneWireMaster.Reset();

            _oneWireMaster.WriteByte(RomCommand.MATCH);

            foreach (var item in OneWireAddress)
            {
                _oneWireMaster.WriteByte(item);
            }
        }

        /// <summary>
        /// Compares crc calculated from the first 8 bytes of the scratchpad to the crc in the 9th byte of the scratchpad
        /// </summary>
        /// <returns>True if the calculated crc matches the received crc, false otherwise</returns>
        private bool CalculateCRC()
        {
            byte crc = 0;
            for (int i = 0; i < 8; i++)
            {
                crc = DS2482Channel.CalculateGlobalCrc8(scratchpad[i], crc);
            }
            return (crc == scratchpad[8]) ? true : false;
        }

        Dictionary<sbyte, double> PowerValues = new Dictionary<sbyte, double>() { { 11, 2048 }, { 10, 1024 }, { 9, 512 }, { 8, 256 }, { 7, 128 }, { 6, 64 }, { 5, 32 }, { 4, 16 }, { 3, 8 }, { 2, 4 }, { 1, 2 }, { 0, 1 }, { -1, 0.5 }, { -2, 0.25 }, { -3, 0.125 }, { -4, 0.0625 } };

        class Scratchpad
        {
            public const int ColdJunctionCompensatedThermocoupleTemperatureLSBAndFaultStatus = 0;

            public const int ColdJunctionCompensatedThermocoupleTemperatureMSB = 1;

            public const int InteralColdJunctionTemperatureAndFaultStatusLSB = 2;

            public const int InteralColdJunctionTemperatureAndFaultStatusMSB = 3;

            public const int ConfigurationRegister = 4;

            public const int Reserved1 = 5;

            public const int Reserved2 = 6;

            public const int Reserved3 = 7;

            public const int CRC = 8;

        }

        public class RomCommand
        {
            public const byte SEARCH = 0xF0;
            public const byte READ = 0x33;
            public const byte MATCH = 0x55;
            public const byte SKIP = 0xCC;
        }

        public class FunctionCommand
        {
            public const byte CONVERT_T = 0x44;
            public const byte READ_SCRATCHPAD = 0xBE;
            public const byte READ_POWER_SUPPLY = 0xB4;
        }
    }
}
