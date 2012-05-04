using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class CPUInstructions
    {
        private readonly CPURegisters cpuRegisters;
        private readonly Memory memory;

        public CPUInstructions( CPURegisters cpuRegisters, Memory memory )
        {
            this.cpuRegisters = cpuRegisters;
            this.memory = memory;
        }

        public void INC_n( ref byte value )
        {
            var newValue = (byte)(value + 1);

            value = newValue;

            cpuRegisters.ZFlag = newValue == 0;
            cpuRegisters.NFlag = false;
            cpuRegisters.HFlag = HasHalfCarry(value, 1);
        }

        public void JR_CC_n( bool condition )
        {
            var value = (ushort)(sbyte)GetByteAtProgramCounter();

            if (condition)
            {
                cpuRegisters.PC += value;
            }
        }

        public void JP_CC_nn( bool condition )
        {
            var value = GetUShortAtProgramCounter();

            if (condition)
            {
                cpuRegisters.PC = value;
            }
        }

        public void AND_n( byte value )
        {
            cpuRegisters.A = (byte)(value & cpuRegisters.A);

            cpuRegisters.ZFlag = cpuRegisters.A == 0;
            cpuRegisters.NFlag = false;
            cpuRegisters.HFlag = true;
            cpuRegisters.CFlag = false;
        }

        private byte GetByteAtProgramCounter()
        {
            return memory[cpuRegisters.PC++];
        }

        private ushort GetUShortAtProgramCounter()
        {
            var lowOrder = memory[cpuRegisters.PC++];
            var highOrder = memory[cpuRegisters.PC++];

            return (ushort)((highOrder << 8) | lowOrder);
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
