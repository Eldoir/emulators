using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class Processor
    {
        public Processor()
        {
            Registers = new CPURegisters();
            ROMMemory = new Memory();
        }

        public void LoadROM( byte[] romData )
        {
            this.romData = new byte[romData.Length];

            Array.Copy(romData, this.romData, romData.Length);
        }
        
        public void Reset()
        {
            cycleCount = 0;
            Registers.Reset();
        }

        public void EmulateFrame()
        {
            do 
            {
                byte opcode = romData[Registers.PC];
                Registers.PC++;

                switch ( opcode )
                {
                    case 0x02: // LD (BC),A
                        romData[Registers.BC] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x06: // LD B,n
                        Registers.B = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x0A: // LD A,(BC)
                        Registers.A = romData[Registers.BC];
                        cycleCount += 8;
                        break;
                    case 0x0E: // LD C,n
                        Registers.C = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x12: // LD (DE),A
                        romData[Registers.DE] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x16: // LD D,n
                        Registers.D = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x1A: // LD A,(DE)
                        Registers.A = romData[Registers.DE];
                        cycleCount += 8;
                        break;
                    case 0x1E: // LD E,n
                        Registers.E = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x26: // LD H,n
                        Registers.H = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x2A: // LD A,(HLI) / LD A,(HL+) / LDI A,(HL)
                        Registers.A = romData[Registers.HL++];
                        cycleCount += 8;
                        break;
                    case 0x2E: // LD L,n
                        Registers.L = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x32: // LD (HLD),A / LD (HL-),A / LDD (HL),A
                        romData[Registers.HL--] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x36: // LD (HL),n
                        // TODO Not sure of this statement
                        romData[Registers.HL] = GetByteAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x3A: // LD A,(HLD) / LD A,(HL-) / LDD A,(HL)
                        Registers.A = romData[Registers.HL--];
                        cycleCount += 8;
                        break;
                    case 0x3E: // LD A,#
                        Registers.A = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x40: // LD B,B
                        Registers.B = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x41: // LD B,C
                        Registers.B = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x42: // LD B,D
                        Registers.B = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x43: // LD B,E
                        Registers.B = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x44: // LD B,H
                        Registers.B = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x45: // LD B,L
                        Registers.B = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x46: // LD B,(HL)
                        Registers.B = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x47: // LD B,A
                        Registers.B = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x48: // LD C,B
                        Registers.C = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x49: // LD C,C
                        Registers.C = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x4A: // LD C,D
                        Registers.C = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x4B: // LD C,E
                        Registers.C = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x4C: // LD C,H
                        Registers.C = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x4D: // LD C,L
                        Registers.C = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x4E: // LD C,(HL)
                        Registers.C = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x4F: // LD C,A
                        Registers.C = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x50: // LD D,B
                        Registers.D = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x51: // LD D,C
                        Registers.D = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x52: // LD D,D
                        Registers.D = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x53: // LD D,E
                        Registers.D = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x54: // LD D,H
                        Registers.D = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x55: // LD D,L
                        Registers.D = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x56: // LD D,(HL)
                        Registers.D = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x57: // LD D,A
                        Registers.D = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x58: // LD E,B
                        Registers.E = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x59: // LD E,C
                        Registers.E = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x5A: // LD E,D
                        Registers.E = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x5B: // LD E,E
                        Registers.E = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x5C: // LD E,H
                        Registers.E = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x5D: // LD E,L
                        Registers.E = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x5E: // LD E,(HL)
                        Registers.E = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x5F: // LD E,A
                        Registers.E = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x60: // LD H,B
                        Registers.H = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x61: // LD H,C
                        Registers.H = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x62: // LD H,D
                        Registers.H = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x63: // LD H,E
                        Registers.H = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x64: // LD H,H
                        Registers.H = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x65: // LD H,L
                        Registers.H = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x66: // LD H,(HL)
                        Registers.H = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x67: // LD H,A
                        Registers.H = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x68: // LD L,B
                        Registers.L = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x69: // LD L,C
                        Registers.L = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x6A: // LD L,D
                        Registers.L = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x6B: // LD L,E
                        Registers.L = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x6C: // LD L,H
                        Registers.L = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x6D: // LD L,L
                        Registers.L = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x6E: // LD L,(HL)
                        Registers.L = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x6F: // LD L,A
                        Registers.L = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x70: // LD (HL),B
                        romData[Registers.HL] = Registers.B;
                        cycleCount += 8;
                        break;
                    case 0x71: // LD (HL),C
                        romData[Registers.HL] = Registers.C;
                        cycleCount += 8;
                        break;
                    case 0x72: // LD (HL),D
                        romData[Registers.HL] = Registers.D;
                        cycleCount += 8;
                        break;
                    case 0x73: // LD (HL),E
                        romData[Registers.HL] = Registers.E;
                        cycleCount += 8;
                        break;
                    case 0x74: // LD (HL),H
                        romData[Registers.HL] = Registers.H;
                        cycleCount += 8;
                        break;
                    case 0x75: // LD (HL),L
                        romData[Registers.HL] = Registers.L;
                        cycleCount += 8;
                        break;
                    case 0x77: // LD (HL),A
                        romData[Registers.HL] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x78: // LD A,B
                        Registers.A = Registers.B;
                        cycleCount += 4;
                        break;
                    case 0x79: // LD A,C
                        Registers.A = Registers.C;
                        cycleCount += 4;
                        break;
                    case 0x7A: // LD A,D
                        Registers.A = Registers.D;
                        cycleCount += 4;
                        break;
                    case 0x7B: // LD A,E
                        Registers.A = Registers.E;
                        cycleCount += 4;
                        break;
                    case 0x7C: // LD A,H
                        Registers.A = Registers.H;
                        cycleCount += 4;
                        break;
                    case 0x7D: // LD A,L
                        Registers.A = Registers.L;
                        cycleCount += 4;
                        break;
                    case 0x7E: // LD A,(HL)
                        Registers.A = romData[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x7F: // LD A,A
                        Registers.A = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0xE2: // LD (C),A
                        romData[0xFF00 + Registers.C] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0xEA: // LD (NN),A
                        romData[GetUShortAtProgramCounter()] = Registers.A;
                        cycleCount += 16;
                        break;
                    case 0xF2: // LD A,(C)
                        Registers.A = romData[ 0xFF00 + Registers.C ];
                        cycleCount += 8;
                        break;
                    case 0xFA: // LD A,(NN)
                        Registers.A = romData[ GetUShortAtProgramCounter() ];
                        cycleCount += 16;
                        break;
                }

            } while ( cycleCount <= 70224 );
        }

        private byte GetByteAtProgramCounter()
        {
            return romData[Registers.PC++];
        }

        private ushort GetUShortAtProgramCounter()
        {
            var lowOrder = romData[Registers.PC++];
            var highOrder = romData[Registers.PC++];

            return (ushort)((highOrder << 8) | lowOrder);
        }
        
        public CPURegisters Registers { get; set; }
        public Memory ROMMemory { get; private set; }

        private const int frameDuration = 70224;

        private int cycleCount;
        private byte[] romData;
    }
}
