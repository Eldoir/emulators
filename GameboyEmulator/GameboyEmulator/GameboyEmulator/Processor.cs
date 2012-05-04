using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace GameboyEmulator
{
    public class Processor
    {
        private readonly Memory memory;
        private readonly GPU gpu;
        private readonly Clock clock;
        private readonly CPURegisters registers;
        private readonly CPUInstructions cpuInstructions;

        private int frameCount;
        private StringBuilder sb = new StringBuilder();

        public Processor( Memory memory, CPURegisters cpuRegisters, GPU gpu, Clock clock)
        {
            this.memory = memory;
            this.registers = cpuRegisters;
            this.gpu = gpu;
            this.clock = clock;

            cpuInstructions = new CPUInstructions( cpuRegisters, memory );
        }
        
        public void Initialize()
        {
            registers.Initialize();
        }

        public bool InterruptsAreEnabled { get; private set; }

        public void EmulateFrame()
        {
            clock.Reset();

            do
            {
                var setInterruptsAfterInstruction = mustDisableInterrupts || mustEnableInterrupts;
                
                var opcode = GetByteAtProgramCounter();

                string registersString = "A: {0:X} | F: {1:X} | B: {2:X} | C: {3:X} | D: {4:X} | E: {5:X} | H: {6:X} | L: {7:X} | PC: {8} | SP: {9}";

                string before = string.Format( CultureInfo.InvariantCulture, registersString, registers.A, registers.F, registers.B, registers.C, registers.D, registers.E, registers.H, registers.L, registers.PC, registers.SP );

                switch (opcode)
                {
                    case 0x00: // NOP
                        cycleCount = 4;
                        break;
                    case 0x01: // LD BC,nn
                        registers.BC = GetUShortAtProgramCounter();
                        cycleCount = 12;
                        break;
                    case 0x02: // LD (BC),A
                        memory[registers.BC] = registers.A;
                        cycleCount = 8;
                        break;
                    case 0x03: // INC BC
                        registers.BC++;
                        cycleCount = 8;
                        break;
                    case 0x04: // INC B
                        {
                            cpuInstructions.INC_n(ref registers.B);

                            cycleCount = 4;
                        }
                        break;
                    case 0x05: // DEC B
                        {
                            var newValue = (byte)(registers.B - 1);

                            registers.B = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x06: // LD B,n
                        registers.B = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x07: // RLCA
                        {
                            var bit7 = (registers.A & (1 << 7)) != 0;

                            registers.A = (byte)((registers.A << 1) | (registers.A >> 7));

                            registers.ZFlag = registers.A == 0;
                            registers.NFlag = false;
                            registers.HFlag = false;
                            registers.CFlag = bit7;

                            cycleCount = 4;
                        }
                        break;
                    case 0x08: // LD (nn), SP
                        WriteUShortAtProgramCounter(registers.SP);
                        cycleCount = 20;
                        break;
                    case 0x09: // ADD HL,BC
                        {
                            registers.NFlag = false;
                            registers.CFlag = ((registers.HL + registers.BC) & 0xFFFF) > 0xFFFF;
                            registers.HFlag = (registers.HL & 0x0FFF) + (registers.BC & 0x0FFF) > 0x0FFF;

                            registers.HL += registers.BC;

                            cycleCount = 8;
                        }
                        break;
                    case 0x0A: // LD A,(BC)
                        registers.A = memory[registers.BC];
                        cycleCount = 8;
                        break;
                    case 0x0B: // DEC BC
                        registers.BC--;
                        cycleCount = 8;
                        break;
                    case 0x0C: // INC C
                        {
                            cpuInstructions.INC_n( ref registers.C );

                            cycleCount = 4;
                        }
                        break;
                    case 0x0D: // DEC C
                        {
                            var newValue = (byte)(registers.C - 1);

                            registers.C = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x0E: // LD C,n
                        registers.C = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x0F: // RRCA
                        {
                            var bit0 = (registers.A & (1 << 0)) != 0;

                            registers.A = (byte)((registers.A >> 1) | (registers.A << 7));

                            registers.ZFlag = registers.A == 0;
                            registers.NFlag = false;
                            registers.HFlag = false;
                            registers.CFlag = bit0;

                            cycleCount = 4;
                        }
                        break;
                    case 0x10:
                        {
                            var nextOpCode = GetByteAtProgramCounter();

                            switch (nextOpCode)
                            {
                                case 0x00: // STOP
                                    cycleCount = 4;
                                    break;
                            }
                        }
                        break;
                    case 0x11: // LD DE,nn
                        registers.DE = GetUShortAtProgramCounter();
                        cycleCount = 12;
                        break;
                    case 0x12: // LD (DE),A
                        memory[registers.DE] = registers.A;
                        cycleCount = 8;
                        break;
                    case 0x13: // INC DE
                        registers.DE++;
                        cycleCount = 8;
                        break;
                    case 0x14: // INC D
                        {
                            cpuInstructions.INC_n( ref registers.D );

                            cycleCount = 4;
                        }
                        break;
                    case 0x15: // DEC D
                        {
                            var newValue = (byte)(registers.D - 1);

                            registers.D = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x16: // LD D,n
                        registers.D = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x17: // RLA
                        {
                            var bit7 = (registers.A & (1 << 7)) != 0;

                            registers.A = (byte)((registers.A << 1) | (registers.CFlag ? 0x01 : 0x00));

                            registers.ZFlag = registers.A == 0;
                            registers.NFlag = false;
                            registers.HFlag = false;
                            registers.CFlag = bit7;

                            cycleCount = 4;
                        }
                        break;
                    case 0x18: // JR n
                        {
                            registers.PC += (ushort) ( 2 + GetByteAtProgramCounter() );

                            cycleCount = 8;
                        }
                        break;
                    case 0x19: // ADD HL,DE
                        {
                            registers.NFlag = false;
                            registers.CFlag = ((registers.HL + registers.DE) & 0xFFFF) > 0xFFFF;
                            registers.HFlag = (registers.HL & 0x0FFF) + (registers.DE & 0x0FFF) > 0x0FFF;

                            registers.HL += registers.DE;

                            cycleCount = 8;
                        }
                        break;
                    case 0x1A: // LD A,(DE)
                        registers.A = memory[registers.DE];
                        cycleCount = 8;
                        break;
                    case 0x1B: // DEC DE
                        registers.DE--;
                        cycleCount = 8;
                        break;
                    case 0x1C: // INC E
                        {
                            cpuInstructions.INC_n( ref registers.E );

                            cycleCount = 4;
                        }
                        break;
                    case 0x1D: // DEC E
                        {
                            var newValue = (byte)(registers.E - 1);

                            registers.E = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x1E: // LD E,n
                        registers.E = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x1F: // RRA
                        {
                            var bit0 = (registers.A & (1 << 0)) != 0;

                            registers.A = (byte)((registers.A >> 1) | (registers.CFlag ? 0x80 : 0x00));

                            registers.ZFlag = registers.A == 0;
                            registers.NFlag = false;
                            registers.HFlag = false;
                            registers.CFlag = bit0;

                            cycleCount = 4;
                        }
                        break;
                    case 0x20: // JR NZ, n
                        {
                            cpuInstructions.JR_CC_n( !registers.ZFlag );

                            cycleCount = 8;
                        }
                        break;
                    case 0x21: // LD HL,nn
                        registers.HL = GetUShortAtProgramCounter();
                        cycleCount = 12;
                        break;
                    case 0x22: // LD (HLI),A / LD (HL+),A / LDI (HL),A
                        memory[registers.HL] = registers.A;
                        registers.HL++;
                        cycleCount = 8;
                        break;
                    case 0x23: // INC HL
                        registers.HL++;
                        cycleCount = 8;
                        break;
                    case 0x24: // INC H
                        {
                            cpuInstructions.INC_n(ref registers.H);

                            cycleCount = 4;
                        }
                        break;
                    case 0x25: // DEC H
                        {
                            var newValue = (byte)(registers.H - 1);

                            registers.H = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x26: // LD H,n
                        registers.H = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x27: // DAA
                        {
                            /*
                             --------------------------------------------------------------------------------
                            |           | C Flag  | HEX value in | H Flag | HEX value in | Number  | C flag|
                            | Operation | Before  | upper digit  | Before | lower digit  | added   | After |
                            |           | DAA     | (bit 7-4)    | DAA    | (bit 3-0)    | to byte | DAA   |
                            |------------------------------------------------------------------------------|
                            |           |    0    |     0-9      |   0    |     0-9      |   00    |   0   |
                            |   ADD     |    0    |     0-8      |   0    |     A-F      |   06    |   0   |
                            |           |    0    |     0-9      |   1    |     0-3      |   06    |   0   |
                            |   ADC     |    0    |     A-F      |   0    |     0-9      |   60    |   1   |
                            |           |    0    |     9-F      |   0    |     A-F      |   66    |   1   |
                            |   INC     |    0    |     A-F      |   1    |     0-3      |   66    |   1   |
                            |           |    1    |     0-2      |   0    |     0-9      |   60    |   1   |
                            |           |    1    |     0-2      |   0    |     A-F      |   66    |   1   |
                            |           |    1    |     0-3      |   1    |     0-3      |   66    |   1   |
                            |------------------------------------------------------------------------------|
                            |   SUB     |    0    |     0-9      |   0    |     0-9      |   00    |   0   |
                            |   SBC     |    0    |     0-8      |   1    |     6-F      |   FA    |   0   |
                            |   DEC     |    1    |     7-F      |   0    |     0-9      |   A0    |   1   |
                            |   NEG     |    1    |     6-F      |   1    |     6-F      |   9A    |   1   |
                            |------------------------------------------------------------------------------|
                             */

                            if (registers.NFlag) // Substraction
                            {
                                if (!registers.CFlag)
                                {
                                    if (registers.HFlag)
                                    {
                                        registers.A += 0xFA;
                                    }
                                }
                                else
                                {
                                    if (!registers.HFlag)
                                    {
                                        registers.A += 0xA0;
                                    }
                                    else
                                    {
                                        registers.A += 0x9A;
                                    }
                                }
                            }
                            else
                            {
                                if (registers.HFlag
                                    || (registers.A & 0x0F) > 0x09)
                                {
                                    registers.A += 0x06;
                                }

                                if (registers.CFlag
                                    || (registers.A & 0xF0) > 0x90)
                                {
                                    registers.A += 0x60;
                                    registers.CFlag = true;
                                }
                            }

                            registers.HFlag = false;
                            registers.ZFlag = registers.A == 0;

                            cycleCount = 4;
                        }
                        break;
                    case 0x28: // JR Z, n
                        {
                            cpuInstructions.JR_CC_n(registers.ZFlag); 

                            cycleCount = 8;
                        }
                        break;
                    case 0x29: // ADD HL,HL
                        {
                            registers.NFlag = false;
                            registers.CFlag = ((registers.HL + registers.HL) & 0xFFFF) > 0xFFFF;
                            registers.HFlag = (registers.HL & 0x0FFF) + (registers.HL & 0x0FFF) > 0x0FFF;

                            registers.HL <<= 1;

                            cycleCount = 8;
                        }
                        break;
                    case 0x2A: // LD A,(HLI) / LD A,(HL+) / LDI A,(HL)
                        registers.A = memory[registers.HL++];
                        cycleCount = 8;
                        break;
                    case 0x2B: // DEC HL
                        registers.HL--;
                        cycleCount = 8;
                        break;
                    case 0x2C: // INC L
                        {
                            cpuInstructions.INC_n(ref registers.L);

                            cycleCount = 4;
                        }
                        break;
                    case 0x2D: // DEC L
                        {
                            var newValue = (byte)(registers.L - 1);

                            registers.L = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x2E: // LD L,n
                        registers.L = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x2F: // CPL
                        {
                            registers.A = (byte)~registers.A;

                            registers.NFlag = true;
                            registers.HFlag = true;

                            cycleCount = 4;
                        }
                        break;
                    case 0x30: // JR NC, n
                        {
                            cpuInstructions.JR_CC_n(!registers.CFlag);

                            cycleCount = 8;
                        }
                        break;
                    case 0x31: // LD SP,nn
                        registers.SP = GetUShortAtProgramCounter();
                        cycleCount = 12;
                        break;
                    case 0x32: // LD (HLD),A / LD (HL-),A / LDD (HL),A
                        memory[registers.HL--] = registers.A;
                        cycleCount = 8;
                        break;
                    case 0x33: // INC SP
                        registers.SP++;
                        cycleCount = 8;
                        break;
                    case 0x34: // INC (HL)
                        {
                            var temp = memory[ registers.HL ];
                            
                            cpuInstructions.INC_n( ref temp );

                            memory[registers.HL] = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x35: // DEC (HL)
                        {
                            var regValue = memory[registers.HL];
                            var newValue = (byte)(regValue - 1);

                            memory[registers.HL] = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(regValue, 1);

                            cycleCount = 12;
                        }
                        break;
                    case 0x36: // LD (HL),n
                        // TODO Not sure of this statement
                        memory[registers.HL] = GetByteAtProgramCounter();
                        cycleCount = 12;
                        break;
                    case 0x37: // SCF
                        {
                            registers.CFlag = true;

                            registers.NFlag = false;
                            registers.HFlag = false;

                            cycleCount = 4;
                        }
                        break;
                    case 0x38: // JR C, n
                        {
                            cpuInstructions.JR_CC_n(registers.CFlag);

                            cycleCount = 8;
                        }
                        break;
                    case 0x39: // ADD HL,SP
                        {
                            registers.NFlag = false;
                            registers.CFlag = ((registers.HL + registers.SP) & 0xFFFF) > 0xFFFF;
                            registers.HFlag = (registers.HL & 0x0FFF) + (registers.SP & 0x0FFF) > 0x0FFF;

                            registers.HL += registers.SP;

                            cycleCount = 8;
                        }
                        break;
                    case 0x3A: // LD A,(HLD) / LD A,(HL-) / LDD A,(HL)
                        registers.A = memory[registers.HL--];
                        cycleCount = 8;
                        break;
                    case 0x3B: // DEC BC
                        registers.SP--;
                        cycleCount = 8;
                        break;
                    case 0x3C: // INC A
                        {
                            cpuInstructions.INC_n(ref registers.A);

                            cycleCount = 4;
                        }
                        break;
                    case 0x3D: // DEC A
                        {
                            var newValue = (byte)(registers.A - 1);

                            registers.A = newValue;

                            registers.ZFlag = newValue == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount = 4;
                        }
                        break;
                    case 0x3E: // LD A,#
                        registers.A = GetByteAtProgramCounter();
                        cycleCount = 8;
                        break;
                    case 0x3F: // CCF
                        {
                            registers.CFlag = !registers.CFlag;

                            registers.NFlag = false;
                            registers.HFlag = false;

                            cycleCount = 4;
                        }
                        break;
                    case 0x40: // LD B,B
                        registers.B = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x41: // LD B,C
                        registers.B = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x42: // LD B,D
                        registers.B = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x43: // LD B,E
                        registers.B = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x44: // LD B,H
                        registers.B = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x45: // LD B,L
                        registers.B = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x46: // LD B,(HL)
                        registers.B = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x47: // LD B,A
                        registers.B = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x48: // LD C,B
                        registers.C = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x49: // LD C,C
                        registers.C = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x4A: // LD C,D
                        registers.C = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x4B: // LD C,E
                        registers.C = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x4C: // LD C,H
                        registers.C = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x4D: // LD C,L
                        registers.C = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x4E: // LD C,(HL)
                        registers.C = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x4F: // LD C,A
                        registers.C = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x50: // LD D,B
                        registers.D = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x51: // LD D,C
                        registers.D = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x52: // LD D,D
                        registers.D = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x53: // LD D,E
                        registers.D = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x54: // LD D,H
                        registers.D = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x55: // LD D,L
                        registers.D = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x56: // LD D,(HL)
                        registers.D = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x57: // LD D,A
                        registers.D = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x58: // LD E,B
                        registers.E = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x59: // LD E,C
                        registers.E = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x5A: // LD E,D
                        registers.E = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x5B: // LD E,E
                        registers.E = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x5C: // LD E,H
                        registers.E = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x5D: // LD E,L
                        registers.E = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x5E: // LD E,(HL)
                        registers.E = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x5F: // LD E,A
                        registers.E = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x60: // LD H,B
                        registers.H = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x61: // LD H,C
                        registers.H = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x62: // LD H,D
                        registers.H = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x63: // LD H,E
                        registers.H = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x64: // LD H,H
                        registers.H = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x65: // LD H,L
                        registers.H = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x66: // LD H,(HL)
                        registers.H = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x67: // LD H,A
                        registers.H = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x68: // LD L,B
                        registers.L = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x69: // LD L,C
                        registers.L = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x6A: // LD L,D
                        registers.L = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x6B: // LD L,E
                        registers.L = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x6C: // LD L,H
                        registers.L = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x6D: // LD L,L
                        registers.L = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x6E: // LD L,(HL)
                        registers.L = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x6F: // LD L,A
                        registers.L = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x70: // LD (HL),B
                        memory[registers.HL] = registers.B;
                        cycleCount = 8;
                        break;
                    case 0x71: // LD (HL),C
                        memory[registers.HL] = registers.C;
                        cycleCount = 8;
                        break;
                    case 0x72: // LD (HL),D
                        memory[registers.HL] = registers.D;
                        cycleCount = 8;
                        break;
                    case 0x73: // LD (HL),E
                        memory[registers.HL] = registers.E;
                        cycleCount = 8;
                        break;
                    case 0x74: // LD (HL),H
                        memory[registers.HL] = registers.H;
                        cycleCount = 8;
                        break;
                    case 0x75: // LD (HL),L
                        memory[registers.HL] = registers.L;
                        cycleCount = 8;
                        break;
                    case 0x76: // HALT
                        cycleCount = 4;
                        break;
                    case 0x77: // LD (HL),A
                        memory[registers.HL] = registers.A;
                        cycleCount = 8;
                        break;
                    case 0x78: // LD A,B
                        registers.A = registers.B;
                        cycleCount = 4;
                        break;
                    case 0x79: // LD A,C
                        registers.A = registers.C;
                        cycleCount = 4;
                        break;
                    case 0x7A: // LD A,D
                        registers.A = registers.D;
                        cycleCount = 4;
                        break;
                    case 0x7B: // LD A,E
                        registers.A = registers.E;
                        cycleCount = 4;
                        break;
                    case 0x7C: // LD A,H
                        registers.A = registers.H;
                        cycleCount = 4;
                        break;
                    case 0x7D: // LD A,L
                        registers.A = registers.L;
                        cycleCount = 4;
                        break;
                    case 0x7E: // LD A,(HL)
                        registers.A = memory[registers.HL];
                        cycleCount = 8;
                        break;
                    case 0x7F: // LD A,A
                        registers.A = registers.A;
                        cycleCount = 4;
                        break;
                    case 0x80: // ADD A,B
                        {
                            var temp = (byte)(registers.A + registers.B);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.B);
                            registers.CFlag = HasCarry(registers.A, registers.B);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x81: // ADD A,C
                        {
                            var temp = (byte)(registers.A + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.C);
                            registers.CFlag = HasCarry(registers.A, registers.C);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x82: // ADD A,D
                        {
                            var temp = (byte)(registers.A + registers.D);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.D);
                            registers.CFlag = HasCarry(registers.A, registers.D);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x83: // ADD A,E
                        {
                            var temp = (byte)(registers.A + registers.E);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.E);
                            registers.CFlag = HasCarry(registers.A, registers.E);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x84: // ADD A,H
                        {
                            var temp = (byte)(registers.A + registers.H);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.H);
                            registers.CFlag = HasCarry(registers.A, registers.H);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x85: // ADD A,L
                        {
                            var temp = (byte)(registers.A + registers.L);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.L);
                            registers.CFlag = HasCarry(registers.A, registers.L);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x86: // ADD A,(HL)
                        {
                            var regValue = memory[registers.HL];
                            var temp = (byte)(registers.A + regValue);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, regValue);
                            registers.CFlag = HasCarry(registers.A, regValue);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0x87: // ADD A,A
                        {
                            var temp = (byte)(registers.A + registers.A);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.A);
                            registers.CFlag = HasCarry(registers.A, registers.A);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x88: // ADC A,B
                        {
                            var temp = (byte)(registers.A + registers.B + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.B, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x89: // ADC A,C
                        {
                            var temp = (byte)(registers.A + registers.C + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.C, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x8A: // ADC A,D
                        {
                            var temp = (byte)(registers.A + registers.D + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.D, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x8B: // ADC A,E
                        {
                            var temp = (byte)(registers.A + registers.E + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.E, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x8C: // ADC A,H
                        {
                            var temp = (byte)(registers.A + registers.H + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.H, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x8D: // ADC A,L
                        {
                            var temp = (byte)(registers.A + registers.L + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.L, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x8E: // ADC A,(HL)
                        {
                            var regValue = memory[registers.HL];
                            var temp = (byte)(registers.A + regValue + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, regValue, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0x8F: // ADC A,A
                        {
                            var temp = (byte)(registers.A + registers.A + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, registers.A, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x90: // SUB A,B
                        {
                            var temp = (byte)(registers.A - registers.B);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.B);
                            registers.CFlag = !HasBorrow(registers.A, registers.B);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x91: // SUB A,C
                        {
                            var temp = (byte)(registers.A - registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.C);
                            registers.CFlag = !HasBorrow(registers.A, registers.C);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x92: // SUB A,D
                        {
                            var temp = (byte)(registers.A - registers.D);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.D);
                            registers.CFlag = !HasBorrow(registers.A, registers.D);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x93: // SUB A,E
                        {
                            var temp = (byte)(registers.A - registers.E);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.E);
                            registers.CFlag = !HasBorrow(registers.A, registers.E);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x94: // SUB A,H
                        {
                            var temp = (byte)(registers.A - registers.H);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.H);
                            registers.CFlag = !HasBorrow(registers.A, registers.H);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x95: // SUB A,L
                        {
                            var temp = (byte)(registers.A - registers.L);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.L);
                            registers.CFlag = !HasBorrow(registers.A, registers.L);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x96: // SUB A,(HL)
                        {
                            var regValue = memory[registers.HL];
                            var temp = (byte)(registers.A - regValue);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, regValue);
                            registers.CFlag = !HasBorrow(registers.A, regValue);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0x97: // SUB A,A
                        {
                            var temp = (byte)(registers.A - registers.A);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.A);
                            registers.CFlag = !HasBorrow(registers.A, registers.A);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x98: // SBC A,B
                        {
                            var toSubstract = (byte)(registers.B + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x99: // SBC A,C
                        {
                            var toSubstract = (byte)(registers.C + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x9A: // SBC A,D
                        {
                            var toSubstract = (byte)(registers.D + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x9B: // SBC A,E
                        {
                            var toSubstract = (byte)(registers.E + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x9C: // SBC A,H
                        {
                            var toSubstract = (byte)(registers.H + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x9D: // SBC A,L
                        {
                            var toSubstract = (byte)(registers.L + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0x9E: // SBC A,(HL)
                        {
                            var toSubstract = (byte)(memory[registers.HL] - registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0x9F: // SBC A,A
                        {
                            var toSubstract = (byte)(registers.A + registers.C);
                            var temp = (byte)(registers.A - toSubstract);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, toSubstract);
                            registers.CFlag = !HasBorrow(registers.A, toSubstract);

                            registers.A = temp;

                            cycleCount = 4;
                        }
                        break;
                    case 0xA0: // AND A,B
                        cpuInstructions.AND_n( registers.B );

                        cycleCount = 4;
                        break;
                    case 0xA1: // AND A,C
                        cpuInstructions.AND_n(registers.C);

                        cycleCount = 4;
                        break;
                    case 0xA2: // AND A,D
                        cpuInstructions.AND_n(registers.D);

                        cycleCount = 4;
                        break;
                    case 0xA3: // AND A,E
                        cpuInstructions.AND_n(registers.E);

                        cycleCount = 4;
                        break;
                    case 0xA4: // AND A,H
                        cpuInstructions.AND_n(registers.H);

                        cycleCount = 4;
                        break;
                    case 0xA5: // AND A,L
                        cpuInstructions.AND_n(registers.L);

                        cycleCount = 4;
                        break;
                    case 0xA6: // AND A,(HL)
                        cpuInstructions.AND_n(memory[registers.HL]);

                        cycleCount = 8;
                        break;
                    case 0xA7: // AND A,A
                        cpuInstructions.AND_n(registers.A);

                        cycleCount = 4;
                        break;
                    case 0xA8: // XOR B
                        registers.A = (byte)(registers.A ^ registers.B);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xA9: // XOR C
                        registers.A = (byte)(registers.A ^ registers.C);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAA: // XOR D
                        registers.A = (byte)(registers.A ^ registers.D);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAB: // XOR E
                        registers.A = (byte)(registers.A ^ registers.E);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAC: // XOR H
                        registers.A = (byte)(registers.A ^ registers.H);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAD: // XOR L
                        registers.A = (byte)(registers.A ^ registers.L);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAE: // XOR (HL)
                        registers.A = (byte)(registers.A ^ memory[registers.HL]);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xAF: // XOR A
                        registers.A = (byte)(registers.A ^ registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB0: // OR A,B
                        registers.A = (byte)(registers.B | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB1: // OR A,C
                        registers.A = (byte)(registers.C | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB2: // OR A,D
                        registers.A = (byte)(registers.D | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB3: // OR A,E
                        registers.A = (byte)(registers.E | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB4: // OR A,H
                        registers.A = (byte)(registers.H | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB5: // OR A,L
                        registers.A = (byte)(registers.L | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB6: // OR A,(HL)
                        registers.A = (byte)(memory[registers.HL] | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 8;
                        break;
                    case 0xB7: // OR A,A
                        registers.A = (byte)(registers.A | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xB8: // CP B
                        {
                            registers.ZFlag = registers.A == registers.B;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.B);
                            registers.CFlag = registers.A < registers.B;

                            cycleCount = 4;
                        }
                        break;
                    case 0xB9: // CP C
                        {
                            registers.ZFlag = registers.A == registers.C;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.C);
                            registers.CFlag = registers.A < registers.C;

                            cycleCount = 4;
                        }
                        break;
                    case 0xBA: // CP D
                        {
                            registers.ZFlag = registers.A == registers.D;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.D);
                            registers.CFlag = registers.A < registers.D;

                            cycleCount = 4;
                        }
                        break;
                    case 0xBB: // CP E
                        {
                            registers.ZFlag = registers.A == registers.E;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.E);
                            registers.CFlag = registers.A < registers.E;

                            cycleCount = 4;
                        }
                        break;
                    case 0xBC: // CP H
                        {
                            registers.ZFlag = registers.A == registers.H;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.H);
                            registers.CFlag = registers.A < registers.H;

                            cycleCount = 4;
                        }
                        break;
                    case 0xBD: // CP L
                        {
                            registers.ZFlag = registers.A == registers.L;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.L);
                            registers.CFlag = registers.A < registers.L;

                            cycleCount = 4;
                        }
                        break;
                    case 0xBE: // CP (HL)
                        {
                            var temp = memory[registers.HL];

                            registers.ZFlag = registers.A == temp;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, temp);
                            registers.CFlag = registers.A < temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0xBF: // CP A
                        {
                            registers.ZFlag = registers.A == registers.A;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, registers.A);
                            registers.CFlag = registers.A < registers.A;

                            cycleCount = 4;
                        }
                        break;
                    case 0xC0: // RET NZ
                        {
                            if (!registers.ZFlag)
                            {
                                RET();
                            }
                            cycleCount = 8;
                        }
                        break;
                    case 0xC1: // POP BC
                        {
                            registers.B = memory[registers.SP++];
                            registers.C = memory[registers.SP++];
                            cycleCount = 12;
                        }

                        break;
                    case 0xC2: // JP NZ, nn
                        {
                            cpuInstructions.JP_CC_nn( !registers.ZFlag );

                            cycleCount = 12;
                        }
                        break;
                    case 0xC3: // JP nn
                        {
                            registers.PC = GetUShortAtProgramCounter();

                            cycleCount = 12;
                        }
                        break;
                    case 0xC4: // CALL NZ,nn
                        {
                            if (!registers.ZFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount = 12;
                        }
                        break;
                    case 0xC5: // PUSH BC
                        {
                            memory[--registers.SP] = registers.B;
                            memory[--registers.SP] = registers.C;
                            cycleCount = 16;
                        }
                        break;
                    case 0xC6: // ADD A,n
                        {
                            var regValue = memory[registers.PC++];
                            var temp = (byte)(registers.A + regValue);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, regValue);
                            registers.CFlag = HasCarry(registers.A, regValue);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0xC7: // RST 00H
                        {
                            RST_n(0x00);
                            cycleCount = 32;
                        }
                        break;
                    case 0xC8: // RET Z
                        {
                            if (registers.ZFlag)
                            {
                                RET();
                            }
                            cycleCount = 8;
                        }
                        break;
                    case 0xC9: // RET
                        {
                            RET();

                            cycleCount = 8;
                        }
                        break;
                    case 0xCA: // JP Z, nn
                        {
                            cpuInstructions.JP_CC_nn(registers.ZFlag);

                            cycleCount = 12;
                        }
                        break;
                    case 0xCB:
                        {
                            var nextOpCode = GetByteAtProgramCounter();

                            switch (nextOpCode)
                            {
                                case 0x00: // RLC B
                                    {
                                        var bit7 = (registers.B & (1 << 7)) != 0;

                                        registers.B = (byte)((registers.B << 1) | (registers.B >> 7));

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x01: // RLC C
                                    {
                                        var bit7 = (registers.C & (1 << 7)) != 0;

                                        registers.C = (byte)((registers.C << 1) | (registers.C >> 7));

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x02: // RLC D
                                    {
                                        var bit7 = (registers.D & (1 << 7)) != 0;

                                        registers.C = (byte)((registers.D << 1) | (registers.D >> 7));

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x03: // RLC E
                                    {
                                        var bit7 = (registers.E & (1 << 7)) != 0;

                                        registers.C = (byte)((registers.E << 1) | (registers.E >> 7));

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x04: // RLC H
                                    {
                                        var bit7 = (registers.H & (1 << 7)) != 0;

                                        registers.C = (byte)((registers.H << 1) | (registers.H >> 7));

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x05: // RLC L
                                    {
                                        var bit7 = (registers.L & (1 << 7)) != 0;

                                        registers.C = (byte)((registers.L << 1) | (registers.L >> 7));

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x06: // RLC (HL)
                                    {
                                        var memValue = memory[registers.HL];
                                        var bit7 = (memValue & (1 << 7)) != 0;

                                        memory[registers.HL] = (byte)((memValue << 1) | (memValue >> 7));

                                        registers.ZFlag = memValue == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x07: // RLC A
                                    {
                                        var bit7 = (registers.A & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.A << 1) | (registers.A >> 7));

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x08: // RRC B
                                    {
                                        var bit0 = (registers.B & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.B >> 1) | (registers.B << 7));

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x09: // RRC C
                                    {
                                        var bit0 = (registers.C & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.C >> 1) | (registers.C << 7));

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x0A: // RRC D
                                    {
                                        var bit0 = (registers.D & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.D >> 1) | (registers.D << 7));

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x0B: // RRC E
                                    {
                                        var bit0 = (registers.E & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.E >> 1) | (registers.E << 7));

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x0C: // RRC H
                                    {
                                        var bit0 = (registers.H & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.H >> 1) | (registers.H << 7));

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x0D: // RRC L
                                    {
                                        var bit0 = (registers.L & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.L >> 1) | (registers.L << 7));

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x0E: // RRC (HL)
                                    {
                                        var memValue = memory[registers.HL];
                                        var bit0 = (memValue & (1 << 0)) != 0;

                                        memory[registers.HL] = (byte)((memValue >> 1) | (memValue << 7));

                                        registers.ZFlag = memValue == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x0F: // RRC A
                                    {
                                        var bit0 = (registers.A & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.A >> 1) | (registers.A << 7));

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x10: // RL B
                                    {
                                        var bit7 = (registers.B & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.B << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x11: // RL C
                                    {
                                        var bit7 = (registers.C & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.C << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x12: // RL D
                                    {
                                        var bit7 = (registers.D & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.D << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x13: // RL E
                                    {
                                        var bit7 = (registers.E & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.E << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x14: // RL H
                                    {
                                        var bit7 = (registers.H & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.H << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x15: // RL L
                                    {
                                        var bit7 = (registers.L & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.L << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x16: // RL (HL)
                                    {
                                        var memValue = memory[registers.HL];
                                        var bit7 = (memValue & (1 << 7)) != 0;

                                        memory[registers.HL] = (byte)((memValue << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = memValue == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x17: // RL A
                                    {
                                        var bit7 = (registers.A & (1 << 7)) != 0;

                                        registers.A = (byte)((registers.A << 1) | (registers.CFlag ? 0x01 : 0x00));

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x18: // RR B
                                    {
                                        var bit0 = (registers.B & (1 << 0)) != 0;

                                        registers.B = (byte)((registers.B >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x19: // RR C
                                    {
                                        var bit0 = (registers.C & (1 << 0)) != 0;

                                        registers.C = (byte)((registers.C >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x1A: // RR D
                                    {
                                        var bit0 = (registers.D & (1 << 0)) != 0;

                                        registers.D = (byte)((registers.D >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x1B: // RR E
                                    {
                                        var bit0 = (registers.E & (1 << 0)) != 0;

                                        registers.E = (byte)((registers.E >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x1C: // RR H
                                    {
                                        var bit0 = (registers.H & (1 << 0)) != 0;

                                        registers.H = (byte)((registers.H >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x1D: // RR L
                                    {
                                        var bit0 = (registers.L & (1 << 0)) != 0;

                                        registers.L = (byte)((registers.L >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x1E: // RR (HL)
                                    {
                                        var memValue = memory[registers.HL];
                                        var bit0 = (memValue & (1 << 0)) != 0;

                                        memory[registers.HL] = (byte)((memValue >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = memValue == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x1F: // RR A
                                    {
                                        var bit0 = (registers.A & (1 << 0)) != 0;

                                        registers.A = (byte)((registers.A >> 1) | (registers.CFlag ? 0x80 : 0x00));

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x20: // SLA B
                                    {
                                        var bit7 = (registers.B & (1 << 7)) != 0;

                                        registers.B <<= 1;

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x21: // SLA C
                                    {
                                        var bit7 = (registers.C & (1 << 7)) != 0;

                                        registers.C <<= 1;

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x22: // SLA D
                                    {
                                        var bit7 = (registers.D & (1 << 7)) != 0;

                                        registers.D <<= 1;

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x23: // SLA E
                                    {
                                        var bit7 = (registers.E & (1 << 7)) != 0;

                                        registers.E <<= 1;

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x24: // SLA H
                                    {
                                        var bit7 = (registers.H & (1 << 7)) != 0;

                                        registers.H <<= 1;

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x25: // SLA L
                                    {
                                        var bit7 = (registers.L & (1 << 7)) != 0;

                                        registers.L <<= 1;

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x26: // SLA (HL)
                                    {
                                        var bit7 = (memory[registers.HL] & (1 << 7)) != 0;

                                        memory[registers.HL] <<= 1;

                                        registers.ZFlag = memory[registers.HL] == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x27: // SLA A
                                    {
                                        var bit7 = (registers.A & (1 << 7)) != 0;

                                        registers.A <<= 1;

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit7;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x28: // SRA B
                                    {
                                        var bit0 = (registers.B & (1 << 0)) != 0;
                                        var bit7 = (registers.B & (1 << 7));

                                        registers.B = (byte)(bit7 | registers.B >> 1);

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x29: // SRA C
                                    {
                                        var bit0 = (registers.C & (1 << 0)) != 0;
                                        var bit7 = (registers.C & (1 << 7));

                                        registers.C = (byte)(bit7 | registers.C >> 1);

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x2A: // SRA D
                                    {
                                        var bit0 = (registers.D & (1 << 0)) != 0;
                                        var bit7 = (registers.D & (1 << 7));

                                        registers.D = (byte)(bit7 | registers.D >> 1);

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x2B: // SRA E
                                    {
                                        var bit0 = (registers.E & (1 << 0)) != 0;
                                        var bit7 = (registers.E & (1 << 7));

                                        registers.E = (byte)(bit7 | registers.E >> 1);

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x2C: // SRA H
                                    {
                                        var bit0 = (registers.H & (1 << 0)) != 0;
                                        var bit7 = (registers.H & (1 << 7));

                                        registers.H = (byte)(bit7 | registers.H >> 1);

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x2D: // SRA L
                                    {
                                        var bit0 = (registers.L & (1 << 0)) != 0;
                                        var bit7 = (registers.L & (1 << 7));

                                        registers.L = (byte)(bit7 | registers.L >> 1);

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x2E: // SRA (HL)
                                    {
                                        var bit0 = (memory[registers.HL] & (1 << 0)) != 0;
                                        var bit7 = (memory[registers.HL] & (1 << 7));

                                        memory[registers.HL] = (byte)(bit7 | memory[registers.HL] >> 1);

                                        registers.ZFlag = memory[registers.HL] == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x2F: // SRA A
                                    {
                                        var bit0 = (registers.A & (1 << 0)) != 0;
                                        var bit7 = (registers.A & (1 << 7));

                                        registers.A = (byte)(bit7 | registers.A >> 1);

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x30: // SWAP B
                                    {
                                        registers.B = (byte)((registers.B & 0x0F) << 4 | (registers.B & 0xF0) >> 4);

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x31: // SWAP C
                                    {
                                        registers.C = (byte)((registers.C & 0x0F) << 4 | (registers.C & 0xF0) >> 4);

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x32: // SWAP D
                                    {
                                        registers.D = (byte)((registers.D & 0x0F) << 4 | (registers.D & 0xF0) >> 4);

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x33: // SWAP E
                                    {
                                        registers.E = (byte)((registers.E & 0x0F) << 4 | (registers.E & 0xF0) >> 4);

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x34: // SWAP H
                                    {
                                        registers.H = (byte)((registers.H & 0x0F) << 4 | (registers.H & 0xF0) >> 4);

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x35: // SWAP L
                                    {
                                        registers.L = (byte)((registers.L & 0x0F) << 4 | (registers.L & 0xF0) >> 4);

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x36: // SWAP (HL)
                                    {
                                        memory[registers.HL] = (byte)((memory[registers.HL] & 0x0F) << 4 | (memory[registers.HL] & 0xF0) >> 4);

                                        registers.ZFlag = memory[registers.HL] == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x37: // SWAP A
                                    {
                                        registers.A = (byte)((registers.A & 0x0F) << 4 | (registers.A & 0xF0) >> 4);

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = false;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x38: // SRL B
                                    {
                                        var bit0 = (registers.B & (1 << 0)) != 0;

                                        registers.B >>= 1;

                                        registers.ZFlag = registers.B == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x39: // SRL C
                                    {
                                        var bit0 = (registers.C & (1 << 0)) != 0;

                                        registers.C >>= 1;

                                        registers.ZFlag = registers.C == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x3A: // SRL D
                                    {
                                        var bit0 = (registers.D & (1 << 0)) != 0;

                                        registers.D >>= 1;

                                        registers.ZFlag = registers.D == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x3B: // SRL E
                                    {
                                        var bit0 = (registers.E & (1 << 0)) != 0;

                                        registers.E >>= 1;

                                        registers.ZFlag = registers.E == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x3C: // SRL H
                                    {
                                        var bit0 = (registers.H & (1 << 0)) != 0;

                                        registers.H >>= 1;

                                        registers.ZFlag = registers.H == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x3D: // SRL L
                                    {
                                        var bit0 = (registers.L & (1 << 0)) != 0;

                                        registers.L >>= 1;

                                        registers.ZFlag = registers.L == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x3E: // SRL (HL)
                                    {
                                        var bit0 = (memory[registers.HL] & (1 << 0)) != 0;

                                        memory[registers.HL] >>= 1;

                                        registers.ZFlag = memory[registers.HL] == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x3F: // SRL A
                                    {
                                        var bit0 = (registers.A & (1 << 0)) != 0;

                                        registers.A >>= 1;

                                        registers.ZFlag = registers.A == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = false;
                                        registers.CFlag = bit0;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x40: // BIT 0, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x41: // BIT 0, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x42: // BIT 0, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x43: // BIT 0, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x44: // BIT 0, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x45: // BIT 0, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x46: // BIT 0, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x47: // BIT 0, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 0)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x48: // BIT 1, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x49: // BIT 1, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x4A: // BIT 1, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x4B: // BIT 1, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x4C: // BIT 1, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x4D: // BIT 1, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x4E: // BIT 1, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x4F: // BIT 1, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 1)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x50: // BIT 2, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x51: // BIT 2, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x52: // BIT 2, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x53: // BIT 2, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x54: // BIT 2, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x55: // BIT 2, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x56: // BIT 2, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x57: // BIT 2, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 2)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x58: // BIT 3, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x59: // BIT 3, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x5A: // BIT 3, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x5B: // BIT 3, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x5C: // BIT 3, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x5D: // BIT 3, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x5E: // BIT 3, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x5F: // BIT 3, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 3)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x60: // BIT 4, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x61: // BIT 4, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x62: // BIT 4, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x63: // BIT 4, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x64: // BIT 4, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x65: // BIT 4, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x66: // BIT 4, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x67: // BIT 4, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 4)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x68: // BIT 5, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x69: // BIT 5, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x6A: // BIT 5, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x6B: // BIT 5, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x6C: // BIT 5, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x6D: // BIT 5, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x6E: // BIT 5, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x6F: // BIT 5, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 5)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x70: // BIT 6, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x71: // BIT 6, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x72: // BIT 6, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x73: // BIT 6, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x74: // BIT 6, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x75: // BIT 6, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x76: // BIT 6, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x77: // BIT 6, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 6)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x78: // BIT 7, B
                                    {
                                        registers.ZFlag = (registers.B & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x79: // BIT 7, C
                                    {
                                        registers.ZFlag = (registers.C & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x7A: // BIT 7, D
                                    {
                                        registers.ZFlag = (registers.D & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x7B: // BIT 7, E
                                    {
                                        registers.ZFlag = (registers.E & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x7C: // BIT 7, H
                                    {
                                        registers.ZFlag = (registers.H & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x7D: // BIT 7, L
                                    {
                                        registers.ZFlag = (registers.L & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x7E: // BIT 7, (HL)
                                    {
                                        registers.ZFlag = (memory[registers.HL] & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x7F: // BIT 7, A
                                    {
                                        registers.ZFlag = (registers.A & (1 << 7)) == 0;
                                        registers.NFlag = false;
                                        registers.HFlag = true;

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x80: // RES 0, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x81: // RES 0, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x82: // RES 0, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x83: // RES 0, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x84: // RES 0, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x85: // RES 0, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x86: // RES 0, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 0));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x87: // RES 0, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 0));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x88: // RES 1, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x89: // RES 1, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x8A: // RES 1, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x8B: // RES 1, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x8C: // RES 1, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x8D: // RES 1, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x8E: // RES 1, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 1));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x8F: // RES 1, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 1));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x90: // RES 2, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x91: // RES 2, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x92: // RES 2, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x93: // RES 2, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x94: // RES 2, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x95: // RES 2, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x96: // RES 2, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 2));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x97: // RES 2, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 2));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x98: // RES 3, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x99: // RES 3, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x9A: // RES 3, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x9B: // RES 3, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x9C: // RES 3, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x9D: // RES 3, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0x9E: // RES 3, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 3));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0x9F: // RES 3, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 3));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA0: // RES 4, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA1: // RES 4, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA2: // RES 4, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA3: // RES 4, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA4: // RES 4, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA5: // RES 4, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA6: // RES 4, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 4));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xA7: // RES 4, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 4));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA8: // RES 5, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xA9: // RES 5, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xAA: // RES 5, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xAB: // RES 5, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xAC: // RES 5, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xAD: // RES 5, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xAE: // RES 5, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 5));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xAF: // RES 5, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 5));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB0: // RES 6, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB1: // RES 6, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB2: // RES 6, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB3: // RES 6, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB4: // RES 6, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB5: // RES 6, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB6: // RES 6, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 6));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xB7: // RES 6, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 6));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB8: // RES 7, B
                                    {
                                        registers.B = (byte)(registers.B & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xB9: // RES 7, C
                                    {
                                        registers.C = (byte)(registers.C & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xBA: // RES 7, D
                                    {
                                        registers.D = (byte)(registers.D & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xBB: // RES 7, E
                                    {
                                        registers.E = (byte)(registers.E & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xBC: // RES 7, H
                                    {
                                        registers.H = (byte)(registers.H & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xBD: // RES 7, L
                                    {
                                        registers.L = (byte)(registers.L & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xBE: // RES 7, (HL)
                                    {
                                        memory[registers.HL] = (byte)(memory[registers.HL] & ~(1 << 7));

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xBF: // RES 7, A
                                    {
                                        registers.A = (byte)(registers.A & ~(1 << 7));

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC0: // SET 0, B
                                    {
                                        registers.B |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC1: // SET 0, C
                                    {
                                        registers.C |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC2: // SET 0, D
                                    {
                                        registers.D |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC3: // SET 0, E
                                    {
                                        registers.E |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC4: // SET 0, H
                                    {
                                        registers.H |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC5: // SET 0, L
                                    {
                                        registers.L |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC6: // SET 0, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 0);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xC7: // SET 0, A
                                    {
                                        registers.A |= (1 << 0);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC8: // SET 1, B
                                    {
                                        registers.B |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xC9: // SET 1, C
                                    {
                                        registers.C |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xCA: // SET 1, D
                                    {
                                        registers.D |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xCB: // SET 1, E
                                    {
                                        registers.E |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xCC: // SET 1, H
                                    {
                                        registers.H |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xCD: // SET 1, L
                                    {
                                        registers.L |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xCE: // SET 1, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 1);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xCF: // SET 1, A
                                    {
                                        registers.A |= (1 << 1);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD0: // SET 2, B
                                    {
                                        registers.B |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD1: // SET 2, C
                                    {
                                        registers.C |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD2: // SET 2, D
                                    {
                                        registers.D |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD3: // SET 2, E
                                    {
                                        registers.E |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD4: // SET 2, H
                                    {
                                        registers.H |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD5: // SET 2, L
                                    {
                                        registers.L |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD6: // SET 2, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 2);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xD7: // SET 2, A
                                    {
                                        registers.A |= (1 << 2);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD8: // SET 3, B
                                    {
                                        registers.B |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xD9: // SET 3, C
                                    {
                                        registers.C |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xDA: // SET 3, D
                                    {
                                        registers.D |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xDB: // SET 3, E
                                    {
                                        registers.E |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xDC: // SET 3, H
                                    {
                                        registers.H |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xDD: // SET 3, L
                                    {
                                        registers.L |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xDE: // SET 3, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 3);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xDF: // SET 3, A
                                    {
                                        registers.A |= (1 << 3);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE0: // SET 4, B
                                    {
                                        registers.B |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE1: // SET 4, C
                                    {
                                        registers.C |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE2: // SET 4, D
                                    {
                                        registers.D |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE3: // SET 4, E
                                    {
                                        registers.E |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE4: // SET 4, H
                                    {
                                        registers.H |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE5: // SET 4, L
                                    {
                                        registers.L |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE6: // SET 4, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 4);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xE7: // SET 4, A
                                    {
                                        registers.A |= (1 << 4);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE8: // SET 5, B
                                    {
                                        registers.B |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xE9: // SET 5, C
                                    {
                                        registers.C |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xEA: // SET 5, D
                                    {
                                        registers.D |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xEB: // SET 5, E
                                    {
                                        registers.E |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xEC: // SET 5, H
                                    {
                                        registers.H |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xED: // SET 5, L
                                    {
                                        registers.L |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xEE: // SET 5, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 5);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xEF: // SET 5, A
                                    {
                                        registers.A |= (1 << 5);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF0: // SET 6, B
                                    {
                                        registers.B |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF1: // SET 6, C
                                    {
                                        registers.C |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF2: // SET 6, D
                                    {
                                        registers.D |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF3: // SET 6, E
                                    {
                                        registers.E |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF4: // SET 6, H
                                    {
                                        registers.H |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF5: // SET 6, L
                                    {
                                        registers.L |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF6: // SET 6, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 6);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xF7: // SET 6, A
                                    {
                                        registers.A |= (1 << 6);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF8: // SET 7, B
                                    {
                                        registers.B |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xF9: // SET 7, C
                                    {
                                        registers.C |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xFA: // SET 7, D
                                    {
                                        registers.D |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xFB: // SET 7, E
                                    {
                                        registers.E |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xFC: // SET 7, H
                                    {
                                        registers.H |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xFD: // SET 7, L
                                    {
                                        registers.L |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                                case 0xFE: // SET 7, (HL)
                                    {
                                        memory[registers.HL] |= (1 << 7);

                                        cycleCount = 16;
                                    }
                                    break;
                                case 0xFF: // SET 7, A
                                    {
                                        registers.A |= (1 << 7);

                                        cycleCount = 8;
                                    }
                                    break;
                            }
                        }
                        break;
                    case 0xCC: // CALL Z,nn
                        {
                            if (registers.ZFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount = 12;
                        }
                        break;
                    case 0xCD: // CALL nn
                        {
                            CALL_nn();

                            cycleCount = 12;
                        }
                        break;
                    case 0xCE: // ADC A,#
                        {
                            var regValue = GetByteAtProgramCounter();
                            var temp = (byte)(registers.A + regValue + registers.C);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = false;
                            registers.HFlag = HasHalfCarry(registers.A, regValue, registers.C);
                            registers.CFlag = HasCarry(temp);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0xCF: // RST 08H
                        {
                            RST_n(0x08);
                            cycleCount = 32;
                        }
                        break;
                    case 0xD0: // RET NC
                        {
                            if (!registers.CFlag)
                            {
                                RET();
                            }
                            cycleCount = 8;
                        }
                        break;
                    case 0xD1: // POP DE
                        registers.D = memory[registers.SP++];
                        registers.E = memory[registers.SP++];
                        cycleCount = 12;
                        break;
                    case 0xD2: // JP NC, nn
                        {
                            cpuInstructions.JP_CC_nn(!registers.NFlag);

                            cycleCount = 12;
                        }
                        break;
                    case 0xD4: // CALL NC,nn
                        {
                            if (!registers.CFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount = 12;
                        }
                        break;
                    case 0xD5: // PUSH DE
                        memory[--registers.SP] = registers.D;
                        memory[--registers.SP] = registers.E;
                        cycleCount = 16;
                        break;
                    case 0xD6: // SUB A,#
                        {
                            var regValue = GetByteAtProgramCounter();
                            var temp = (byte)(registers.A - regValue);

                            registers.ZFlag = temp == 0;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, regValue);
                            registers.CFlag = !HasBorrow(registers.A, regValue);

                            registers.A = temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0xD7: // RST 10H
                        {
                            RST_n(0x10);
                            cycleCount = 32;
                        }
                        break;
                    case 0xD8: // RET C
                        {
                            if (registers.CFlag)
                            {
                                RET();
                            }
                            cycleCount = 8;
                        }
                        break;
                    case 0xD9: // RETI
                        {
                            RET();
                            EI();
                            
                            cycleCount = 8;
                        }
                        break;
                    case 0xDA: // JP C, nn
                        {
                            cpuInstructions.JP_CC_nn(registers.CFlag);

                            cycleCount = 12;
                        }
                        break;
                    case 0xDC: // CALL C,nn
                        {
                            if (registers.CFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount = 12;
                        }
                        break;
                    case 0xDF: // RST 18H
                        {
                            RST_n(0x18);
                            cycleCount = 32;
                        }
                        break;
                    case 0xE0: // LDH (n),A
                        memory[(ushort)(0xFF00 + GetByteAtProgramCounter())] = registers.A;
                        cycleCount = 12;
                        break;
                    case 0xE1: // POP HL
                        registers.H = memory[registers.SP++];
                        registers.L = memory[registers.SP++];
                        cycleCount = 12;
                        break;
                    case 0xE2: // LD (C),A
                        memory[(ushort)(0xFF00 + registers.C)] = registers.A;
                        cycleCount = 8;
                        break;
                    case 0xE5: // PUSH HL
                        memory[--registers.SP] = registers.H;
                        memory[--registers.SP] = registers.L;
                        cycleCount = 16;
                        break;
                    case 0xE6: // AND A,#
                        cpuInstructions.AND_n( GetByteAtProgramCounter() );
                        cycleCount = 8;
                        break;
                    case 0xE7: // RST 20H
                        {
                            RST_n(0x20);
                            cycleCount = 32;
                        }
                        break;
                    case 0xE8: // ADD SP,n
                        {
                            var value = GetByteAtProgramCounter();

                            registers.ZFlag = false;
                            registers.NFlag = false;
                            registers.CFlag = HasCarry(registers.SP, value);
                            registers.HFlag = HasHalfCarry(registers.SP, value);

                            registers.SP += value;

                            cycleCount = 16; // TODO: check if this should be 8 instead
                        }
                        break;
                    case 0xE9: // JP (HL)
                        {
                            registers.PC = memory[registers.HL];
                            cycleCount = 4;
                        }
                        break;
                    case 0xEA: // LD (NN),A
                        memory[GetUShortAtProgramCounter()] = registers.A;
                        cycleCount = 16;
                        break;
                    case 0xEE: // XOR #
                        registers.A = (byte)(registers.A ^ GetByteAtProgramCounter());

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 4;
                        break;
                    case 0xEF: // RST 28H
                        {
                            RST_n(0x28);
                            cycleCount = 32;
                        }
                        break;
                    case 0xF0: // LDH A,(n)
                        registers.A = memory[(ushort)(0xFF00 + GetByteAtProgramCounter())];
                        cycleCount = 12;
                        break;
                    case 0xF1: // POP AF
                        registers.A = memory[registers.SP++];
                        registers.F = memory[registers.SP++];
                        cycleCount = 12;
                        break;
                    case 0xF2: // LD A,(C)
                        registers.A = memory[(ushort)(0xFF00 + registers.C)];
                        cycleCount = 8;
                        break;
                    case 0xF3: // DI
                        mustDisableInterrupts = true;
                        cycleCount = 4;
                        break;
                    case 0xF5: // PUSH AF
                        memory[--registers.SP] = registers.A;
                        memory[--registers.SP] = registers.F;
                        cycleCount = 16;
                        break;
                    case 0xF6: // OR A,#
                        registers.A = (byte)(GetByteAtProgramCounter() | registers.A);

                        registers.ZFlag = registers.A == 0;
                        registers.NFlag = false;
                        registers.HFlag = false;
                        registers.CFlag = false;

                        cycleCount = 8;
                        break;
                    case 0xF7: // RST 30H
                        {
                            RST_n(0x30);
                            cycleCount = 32;
                        }
                        break;
                    case 0xF8: // LD HL,SP+n / LDHL SP,n
                        var n = memory[GetByteAtProgramCounter()];

                        registers.HL = (ushort)(registers.SP + n);

                        registers.ZFlag = false;
                        registers.NFlag = false;
                        registers.HFlag = HasHalfCarry(registers.SP, n);
                        registers.CFlag = HasCarry(registers.SP, n);

                        cycleCount = 12;
                        break;
                    case 0xF9: // LD SP,HL
                        registers.SP = registers.HL;
                        cycleCount = 8;
                        break;
                    case 0xFA: // LD A,(NN)
                        registers.A = memory[GetUShortAtProgramCounter()];
                        cycleCount = 16;
                        break;
                    case 0xFB: // EI
                        {
                            EI();
                            cycleCount = 4;
                        }
                        break;
                    case 0xFE: // CP #
                        {
                            var temp = GetByteAtProgramCounter();

                            registers.ZFlag = registers.A == temp;
                            registers.NFlag = true;
                            registers.HFlag = !HasHalfBorrow(registers.A, temp);
                            registers.CFlag = registers.A < temp;

                            cycleCount = 8;
                        }
                        break;
                    case 0xFF: // RST 38H
                        {
                            RST_n(0x38);
                            cycleCount = 32;
                        }
                        break;
                }

                string after = string.Format(CultureInfo.InstalledUICulture, registersString, registers.A, registers.F, registers.B, registers.C, registers.D, registers.E, registers.H, registers.L, registers.PC, registers.SP); 
                
                Debug.Assert(cycleCount > 0);

                clock.IncrementCycleCount( cycleCount );
                
                gpu.FrameStep();

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "OpCode : {0:X}", opcode));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CycleCount : {0}", cycleCount));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TotalCycleCount : {0}", clock.CycleCount));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "B : {0:X}", before));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "A : {0:X}", after));
                
                if (setInterruptsAfterInstruction)
                {
                    if (mustDisableInterrupts)
                    {
                        InterruptsAreEnabled = false;
                    }
                    else if (mustEnableInterrupts)
                    {
                        InterruptsAreEnabled = true;
                    }
                    
                    mustEnableInterrupts = mustDisableInterrupts = false;
                }
            } while ( !clock.IsFrameCompleted );

            frameCount++;

            if (frameCount == 5)
            {
                using (var fs = new FileStream("D:\\log.txt", FileMode.Create, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.Write(sb.ToString());
                    }
                }
            }
        }

        private void EI()
        {
            mustEnableInterrupts = true;
        }

        private void RET()
        {
            var nextOpcode = ( ushort ) ( ( memory[ registers.SP++ ] ) | ( memory[ registers.SP++ ] << 8 ) );

            registers.PC = nextOpcode;
        }

        private void RST_n( ushort offset )
        {
            memory[ --registers.SP ] = ( byte ) ( ( registers.PC & 0xFF00 ) >> 8 );
            memory[ --registers.SP ] = ( byte ) ( ( registers.PC & 0X00FF ) );

            registers.PC = ( ushort ) ( 0x0000 + offset );
        }

        private void CALL_nn()
        {
            memory[ --registers.SP ] = ( byte ) ( ( ( registers.PC + 3 ) & 0xFF00 ) >> 8 );
            memory[ --registers.SP ] = ( byte ) ( ( ( registers.PC + 3 ) & 0X00FF ) );

            var nextOpCode = GetUShortAtProgramCounter();
            registers.PC = nextOpCode;
        }

        private byte GetByteAtProgramCounter()
        {
            return memory[ registers.PC++ ];
        }

        private ushort GetUShortAtProgramCounter()
        {
            var lowOrder = memory[ registers.PC++ ];
            var highOrder = memory[ registers.PC++ ];

            return ( ushort ) ( ( highOrder << 8 ) | lowOrder );
        }

        private bool HasCarry( ushort first, ushort second )
        {
            return ( first & 0xFF ) + ( second & 0xFF ) > 0xFF;
        }

        private bool HasCarry( ushort value )
        {
            return (value & 0xFF) > 0xFF;
        }

        private bool HasHalfCarry( ushort first, ushort second )
        {
            return ( first & 0x0F ) + ( second & 0x0F ) > 0x0F;
        }

        private bool HasHalfCarry(ushort first, ushort second, byte third)
        {
            return (first & 0x0F) + (second & 0x0F) + third > 0x0F;
        }

        private bool HasHalfBorrow( ushort first, ushort second )
        {
            return ( first & 0x0F ) < ( second & 0x0f );
        }

        private bool HasBorrow( ushort first, ushort second )
        {
            return first < second;
        }

        private void WriteUShortAtProgramCounter( ushort value )
        {
            memory[ registers.PC++ ] = ( byte ) ( value );
            memory[ registers.PC++ ] = ( byte ) ( value >> 8 );
        }

        private const int frameDuration = 70224;
        private int cycleCount;
        //private byte[] romData;
        private bool mustDisableInterrupts = false;
        private bool mustEnableInterrupts = false;
    }
}