using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rinsen.IoT.OneWire.Tests
{
    // See page 13 of the MAX31850/MAX31851 datasheet for the following test data

    public class MAX31850Tests
    {
        [Theory]
        [InlineData(new byte[] { 0b00000000, 0b01100100, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1600)]
        [InlineData(new byte[] { 0b10000000, 0b00111110, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 1000)]
        [InlineData(new byte[] { 0b01001100, 0b00000110, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 100.75)]
        [InlineData(new byte[] { 0b10010000, 0b00000001, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 25)]
        [InlineData(new byte[] { 0b00000000, 0b00000000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0)]
        [InlineData(new byte[] { 0b11111100, 0b11111111, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, -0.25)]
        [InlineData(new byte[] { 0b11110000, 0b11111111, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, -1.00)]
        [InlineData(new byte[] { 0b01100000, 0b11110000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, -250.00)]
        public void ColdJunctionCompensatedThermocoupleTemperatureCalculations(byte[] scratchpad, double expectedTemp)
        {
            // Arrange
            var max31850 = new MAX31850();

            // Use reflection to set the scratchpad (assuming SetScratchpad is public or has internal access)
            var fieldInfo = typeof(MAX31850).GetField("scratchpad", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(max31850, scratchpad);

            // Use reflection to invoke the private method
            var methodInfo = typeof(MAX31850).GetMethod("GetColdJunctionCompensatedThermocoupleTemperature", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (double)methodInfo.Invoke(max31850, null);

            // Assert
            Assert.Equal(expectedTemp, result);
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b01111111, 0x00, 0x00, 0x00, 0x00 }, 127)]
        [InlineData(new byte[] { 0x00, 0x00, 0b10010000, 0b01100100,  0x00, 0x00, 0x00, 0x00 }, 100.5625)]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b00011001,  0x00, 0x00, 0x00, 0x00 }, 25)]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b00000000,  0x00, 0x00, 0x00, 0x00 }, 0)]
        [InlineData(new byte[] { 0x00, 0x00, 0b11110000, 0b11111111,  0x00, 0x00, 0x00, 0x00 }, -0.0625)]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b11111111,  0x00, 0x00, 0x00, 0x00 }, -1)]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b11101100,  0x00, 0x00, 0x00, 0x00 }, -20)]
        [InlineData(new byte[] { 0x00, 0x00, 0b00000000, 0b11001001,  0x00, 0x00, 0x00, 0x00 }, -55)]
        public void InternalColdJunctionTemperatureCalculations(byte[] scratchpad, double expectedTemp)
        {
            // Arrange
            var max31850 = new MAX31850();

            // Use reflection to set the scratchpad (assuming SetScratchpad is public or has internal access)
            var fieldInfo = typeof(MAX31850).GetField("scratchpad", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(max31850, scratchpad);

            // Use reflection to invoke the private method
            var methodInfo = typeof(MAX31850).GetMethod("GetInternalColdJunctionTemperature", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (double)methodInfo.Invoke(max31850, null);

            // Assert
            Assert.Equal(expectedTemp, result);
        }
    }
}
