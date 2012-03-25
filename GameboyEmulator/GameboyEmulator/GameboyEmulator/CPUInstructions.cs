using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class CPUInstructions
    {
        private readonly CPURegisters cpuRegisters;

        public CPUInstructions( CPURegisters cpuRegisters )
        {
            this.cpuRegisters = cpuRegisters;
        }

        public void INC_n( ref byte value )
        {
            var newValue = (byte)(value + 1);

            value = newValue;

            cpuRegisters.ZFlag = newValue == 0;
            cpuRegisters.NFlag = false;
            cpuRegisters.HFlag = HasHalfCarry(value, 1);
        }

        private bool HasCarry(ushort first, ushort second)
        {
            return (first & 0xFF) + (second & 0xFF) > 0xFF;
        }

        private bool HasCarry(ushort value)
        {
            return (value & 0xFF) > 0xFF;
        }

        private bool HasHalfCarry(ushort first, ushort second)
        {
            return (first & 0x0F) + (second & 0x0F) > 0x0F;
        }

        private bool HasHalfCarry(ushort first, ushort second, byte third)
        {
            return (first & 0x0F) + (second & 0x0F) + third > 0x0F;
        }

        private bool HasHalfBorrow(ushort first, ushort second)
        {
            return (first & 0x0F) < (second & 0x0f);
        }

        private bool HasBorrow(ushort first, ushort second)
        {
            return first < second;
        }
    }
}
