using System;

namespace GameboyEmulator
{
    public class Processor
    {
        private Memory memory;

        public Processor( Memory memory )
        {
            Registers = new CPURegisters();
            this.memory = memory;
        }

        public CPURegisters Registers { get; set; }
        public bool InterruptsAreEnabled { get; private set; }

        public void EmulateFrame()
        {
            do
            {
                var setInterruptsAfterInstruction = mustDisableInterrupts || mustEnableInterrupts;
                
                var opcode = GetByteAtProgramCounter();

                switch (opcode)
                {
                    case 0x00: // NOP
                        cycleCount += 4;
                        break;
                    case 0x01: // LD BC,nn
                        Registers.BC = GetUShortAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x02: // LD (BC),A
                        memory[Registers.BC] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x03: // INC BC
                        Registers.BC++;
                        cycleCount += 8;
                        break;
                    case 0x04: // INC B
                        {
                            var newValue = (byte)(Registers.B + 1);

                            Registers.B = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.B, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x05: // DEC B
                        {
                            var newValue = (byte)(Registers.B - 1);

                            Registers.B = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x06: // LD B,n
                        Registers.B = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x07: // RLCA
                        {
                            var bit7 = (Registers.A & (1 << 7)) != 0;

                            Registers.A = (byte)((Registers.A << 1) | (Registers.A >> 7));

                            Registers.ZFlag = Registers.A == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = false;
                            Registers.CFlag = bit7;

                            cycleCount += 4;
                        }
                        break;
                    case 0x08: // LD (nn), SP
                        WriteUShortAtProgramCounter(Registers.SP);
                        cycleCount += 20;
                        break;
                    case 0x09: // ADD HL,BC
                        {
                            Registers.NFlag = false;
                            Registers.CFlag = ((Registers.HL + Registers.BC) & 0xFFFF) > 0xFFFF;
                            Registers.HFlag = (Registers.HL & 0x0FFF) + (Registers.BC & 0x0FFF) > 0x0FFF;

                            Registers.HL += Registers.BC;

                            cycleCount += 8;
                        }
                        break;
                    case 0x0A: // LD A,(BC)
                        Registers.A = memory[Registers.BC];
                        cycleCount += 8;
                        break;
                    case 0x0B: // DEC BC
                        Registers.BC--;
                        cycleCount += 8;
                        break;
                    case 0x0C: // INC C
                        {
                            var newValue = (byte)(Registers.C + 1);

                            Registers.C = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.C, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x0D: // DEC C
                        {
                            var newValue = (byte)(Registers.C - 1);

                            Registers.C = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x0E: // LD C,n
                        Registers.C = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x0F: // RRCA
                        {
                            var bit0 = (Registers.A & (1 << 0)) != 0;

                            Registers.A = (byte)((Registers.A >> 1) | (Registers.A << 7));

                            Registers.ZFlag = Registers.A == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = false;
                            Registers.CFlag = bit0;

                            cycleCount += 4;
                        }
                        break;
                    case 0x10:
                        {
                            var nextOpCode = GetByteAtProgramCounter();

                            switch (nextOpCode)
                            {
                                case 0x00: // STOP
                                    cycleCount += 4;
                                    break;
                            }
                        }
                        break;
                    case 0x11: // LD DE,nn
                        Registers.DE = GetUShortAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x12: // LD (DE),A
                        memory[Registers.DE] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x13: // INC DE
                        Registers.DE++;
                        cycleCount += 8;
                        break;
                    case 0x14: // INC D
                        {
                            var newValue = (byte)(Registers.D + 1);

                            Registers.D = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.D, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x15: // DEC D
                        {
                            var newValue = (byte)(Registers.D - 1);

                            Registers.D = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x16: // LD D,n
                        Registers.D = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x17: // RLA
                        {
                            var bit7 = (Registers.A & (1 << 7)) != 0;

                            Registers.A = (byte)((Registers.A << 1) | (Registers.CFlag ? 0x01 : 0x00));

                            Registers.ZFlag = Registers.A == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = false;
                            Registers.CFlag = bit7;

                            cycleCount += 4;
                        }
                        break;
                    case 0x18: // JR n
                        {
                            Registers.PC += GetByteAtProgramCounter();

                            cycleCount += 8;
                        }
                        break;
                    case 0x19: // ADD HL,DE
                        {
                            Registers.NFlag = false;
                            Registers.CFlag = ((Registers.HL + Registers.DE) & 0xFFFF) > 0xFFFF;
                            Registers.HFlag = (Registers.HL & 0x0FFF) + (Registers.DE & 0x0FFF) > 0x0FFF;

                            Registers.HL += Registers.DE;

                            cycleCount += 8;
                        }
                        break;
                    case 0x1A: // LD A,(DE)
                        Registers.A = memory[Registers.DE];
                        cycleCount += 8;
                        break;
                    case 0x1B: // DEC DE
                        Registers.DE--;
                        cycleCount += 8;
                        break;
                    case 0x1C: // INC E
                        {
                            var newValue = (byte)(Registers.E + 1);

                            Registers.E = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.E, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x1D: // DEC E
                        {
                            var newValue = (byte)(Registers.E - 1);

                            Registers.E = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x1E: // LD E,n
                        Registers.E = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x1F: // RRA
                        {
                            var bit0 = (Registers.A & (1 << 0)) != 0;

                            Registers.A = (byte)((Registers.A >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                            Registers.ZFlag = Registers.A == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = false;
                            Registers.CFlag = bit0;

                            cycleCount += 4;
                        }
                        break;
                    case 0x20: // JR NZ, n
                        {
                            if (!Registers.ZFlag)
                            {
                                Registers.PC += GetByteAtProgramCounter();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0x21: // LD HL,nn
                        Registers.HL = GetUShortAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x22: // LD (HLI),A / LD (HL+),A / LDI (HL),A
                        memory[Registers.HL++] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x23: // INC HL
                        Registers.HL++;
                        cycleCount += 8;
                        break;
                    case 0x24: // INC H
                        {
                            var newValue = (byte)(Registers.H + 1);

                            Registers.H = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.H, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x25: // DEC H
                        {
                            var newValue = (byte)(Registers.H - 1);

                            Registers.H = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x26: // LD H,n
                        Registers.H = GetByteAtProgramCounter();
                        cycleCount += 8;
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

                            if (Registers.NFlag) // Substraction
                            {
                                if (!Registers.CFlag)
                                {
                                    if (Registers.HFlag)
                                    {
                                        Registers.A += 0xFA;
                                    }
                                }
                                else
                                {
                                    if (!Registers.HFlag)
                                    {
                                        Registers.A += 0xA0;
                                    }
                                    else
                                    {
                                        Registers.A += 0x9A;
                                    }
                                }
                            }
                            else
                            {
                                if (Registers.HFlag
                                    || (Registers.A & 0x0F) > 0x09)
                                {
                                    Registers.A += 0x06;
                                }

                                if (Registers.CFlag
                                    || (Registers.A & 0xF0) > 0x90)
                                {
                                    Registers.A += 0x60;
                                    Registers.CFlag = true;
                                }
                            }

                            Registers.HFlag = false;
                            Registers.ZFlag = Registers.A == 0;

                            cycleCount += 4;
                        }
                        break;
                    case 0x28: // JR Z, n
                        {
                            if (Registers.ZFlag)
                            {
                                Registers.PC += GetByteAtProgramCounter();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0x29: // ADD HL,HL
                        {
                            Registers.NFlag = false;
                            Registers.CFlag = ((Registers.HL + Registers.HL) & 0xFFFF) > 0xFFFF;
                            Registers.HFlag = (Registers.HL & 0x0FFF) + (Registers.HL & 0x0FFF) > 0x0FFF;

                            Registers.HL <<= 1;

                            cycleCount += 8;
                        }
                        break;
                    case 0x2A: // LD A,(HLI) / LD A,(HL+) / LDI A,(HL)
                        Registers.A = memory[Registers.HL++];
                        cycleCount += 8;
                        break;
                    case 0x2B: // DEC HL
                        Registers.HL--;
                        cycleCount += 8;
                        break;
                    case 0x2C: // INC L
                        {
                            var newValue = (byte)(Registers.L + 1);

                            Registers.L = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.L, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x2D: // DEC L
                        {
                            var newValue = (byte)(Registers.L - 1);

                            Registers.L = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x2E: // LD L,n
                        Registers.L = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x2F: // CPL
                        {
                            Registers.A = (byte)~Registers.A;

                            Registers.NFlag = true;
                            Registers.HFlag = true;

                            cycleCount += 4;
                        }
                        break;
                    case 0x30: // JR NC, n
                        {
                            if (!Registers.CFlag)
                            {
                                Registers.PC += GetByteAtProgramCounter();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0x31: // LD SP,nn
                        Registers.SP = GetUShortAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x32: // LD (HLD),A / LD (HL-),A / LDD (HL),A
                        memory[Registers.HL--] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0x33: // INC SP
                        Registers.SP++;
                        cycleCount += 8;
                        break;
                    case 0x34: // INC (HL)
                        {
                            var regValue = memory[Registers.HL];
                            var newValue = (byte)(regValue + 1);

                            memory[Registers.HL] = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(regValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x35: // DEC (HL)
                        {
                            var regValue = memory[Registers.HL];
                            var newValue = (byte)(regValue - 1);

                            memory[Registers.HL] = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(regValue, 1);

                            cycleCount += 12;
                        }
                        break;
                    case 0x36: // LD (HL),n
                        // TODO Not sure of this statement
                        memory[Registers.HL] = GetByteAtProgramCounter();
                        cycleCount += 12;
                        break;
                    case 0x37: // SCF
                        {
                            Registers.CFlag = true;

                            Registers.NFlag = false;
                            Registers.HFlag = false;

                            cycleCount += 4;
                        }
                        break;
                    case 0x38: // JR C, n
                        {
                            if (Registers.CFlag)
                            {
                                Registers.PC += GetByteAtProgramCounter();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0x39: // ADD HL,SP
                        {
                            Registers.NFlag = false;
                            Registers.CFlag = ((Registers.HL + Registers.SP) & 0xFFFF) > 0xFFFF;
                            Registers.HFlag = (Registers.HL & 0x0FFF) + (Registers.SP & 0x0FFF) > 0x0FFF;

                            Registers.HL += Registers.SP;

                            cycleCount += 8;
                        }
                        break;
                    case 0x3A: // LD A,(HLD) / LD A,(HL-) / LDD A,(HL)
                        Registers.A = memory[Registers.HL--];
                        cycleCount += 8;
                        break;
                    case 0x3B: // DEC BC
                        Registers.SP--;
                        cycleCount += 8;
                        break;
                    case 0x3C: // INC A
                        {
                            var newValue = (byte)(Registers.A + 1);

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, 1);

                            Registers.A = newValue;
                            cycleCount += 4;
                        }
                        break;
                    case 0x3D: // DEC A
                        {
                            var newValue = (byte)(Registers.A - 1);

                            Registers.A = newValue;

                            Registers.ZFlag = newValue == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(newValue, 1);

                            cycleCount += 4;
                        }
                        break;
                    case 0x3E: // LD A,#
                        Registers.A = GetByteAtProgramCounter();
                        cycleCount += 8;
                        break;
                    case 0x3F: // CCF
                        {
                            Registers.CFlag = !Registers.CFlag;

                            Registers.NFlag = false;
                            Registers.HFlag = false;

                            cycleCount += 4;
                        }
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
                        Registers.B = memory[Registers.HL];
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
                        Registers.C = memory[Registers.HL];
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
                        Registers.D = memory[Registers.HL];
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
                        Registers.E = memory[Registers.HL];
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
                        Registers.H = memory[Registers.HL];
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
                        Registers.L = memory[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x6F: // LD L,A
                        Registers.L = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x70: // LD (HL),B
                        memory[Registers.HL] = Registers.B;
                        cycleCount += 8;
                        break;
                    case 0x71: // LD (HL),C
                        memory[Registers.HL] = Registers.C;
                        cycleCount += 8;
                        break;
                    case 0x72: // LD (HL),D
                        memory[Registers.HL] = Registers.D;
                        cycleCount += 8;
                        break;
                    case 0x73: // LD (HL),E
                        memory[Registers.HL] = Registers.E;
                        cycleCount += 8;
                        break;
                    case 0x74: // LD (HL),H
                        memory[Registers.HL] = Registers.H;
                        cycleCount += 8;
                        break;
                    case 0x75: // LD (HL),L
                        memory[Registers.HL] = Registers.L;
                        cycleCount += 8;
                        break;
                    case 0x76: // HALT
                        cycleCount += 4;
                        break;
                    case 0x77: // LD (HL),A
                        memory[Registers.HL] = Registers.A;
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
                        Registers.A = memory[Registers.HL];
                        cycleCount += 8;
                        break;
                    case 0x7F: // LD A,A
                        Registers.A = Registers.A;
                        cycleCount += 4;
                        break;
                    case 0x80: // ADD A,B
                        {
                            var temp = (byte)(Registers.A + Registers.B);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.B);
                            Registers.CFlag = HasCarry(Registers.A, Registers.B);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x81: // ADD A,C
                        {
                            var temp = (byte)(Registers.A + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.C);
                            Registers.CFlag = HasCarry(Registers.A, Registers.C);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x82: // ADD A,D
                        {
                            var temp = (byte)(Registers.A + Registers.D);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.D);
                            Registers.CFlag = HasCarry(Registers.A, Registers.D);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x83: // ADD A,E
                        {
                            var temp = (byte)(Registers.A + Registers.E);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.E);
                            Registers.CFlag = HasCarry(Registers.A, Registers.E);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x84: // ADD A,H
                        {
                            var temp = (byte)(Registers.A + Registers.H);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.H);
                            Registers.CFlag = HasCarry(Registers.A, Registers.H);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x85: // ADD A,L
                        {
                            var temp = (byte)(Registers.A + Registers.L);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.L);
                            Registers.CFlag = HasCarry(Registers.A, Registers.L);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x86: // ADD A,(HL)
                        {
                            var regValue = memory[Registers.HL];
                            var temp = (byte)(Registers.A + regValue);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, regValue);
                            Registers.CFlag = HasCarry(Registers.A, regValue);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0x87: // ADD A,A
                        {
                            var temp = (byte)(Registers.A + Registers.A);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.A);
                            Registers.CFlag = HasCarry(Registers.A, Registers.A);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x88: // ADC A,B
                        {
                            var temp = (byte)(Registers.A + Registers.B + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.B, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x89: // ADC A,C
                        {
                            var temp = (byte)(Registers.A + Registers.C + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.C, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x8A: // ADC A,D
                        {
                            var temp = (byte)(Registers.A + Registers.D + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.D, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x8B: // ADC A,E
                        {
                            var temp = (byte)(Registers.A + Registers.E + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.E, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x8C: // ADC A,H
                        {
                            var temp = (byte)(Registers.A + Registers.H + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.H, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x8D: // ADC A,L
                        {
                            var temp = (byte)(Registers.A + Registers.L + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.L, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x8E: // ADC A,(HL)
                        {
                            var regValue = memory[Registers.HL];
                            var temp = (byte)(Registers.A + regValue + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, regValue, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0x8F: // ADC A,A
                        {
                            var temp = (byte)(Registers.A + Registers.A + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, Registers.A, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x90: // SUB A,B
                        {
                            var temp = (byte)(Registers.A - Registers.B);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.B);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.B);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x91: // SUB A,C
                        {
                            var temp = (byte)(Registers.A - Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.C);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.C);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x92: // SUB A,D
                        {
                            var temp = (byte)(Registers.A - Registers.D);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.D);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.D);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x93: // SUB A,E
                        {
                            var temp = (byte)(Registers.A - Registers.E);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.E);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.E);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x94: // SUB A,H
                        {
                            var temp = (byte)(Registers.A - Registers.H);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.H);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.H);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x95: // SUB A,L
                        {
                            var temp = (byte)(Registers.A - Registers.L);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.L);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.L);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x96: // SUB A,(HL)
                        {
                            var regValue = memory[Registers.HL];
                            var temp = (byte)(Registers.A - regValue);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, regValue);
                            Registers.CFlag = !HasBorrow(Registers.A, regValue);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0x97: // SUB A,A
                        {
                            var temp = (byte)(Registers.A - Registers.A);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.A);
                            Registers.CFlag = !HasBorrow(Registers.A, Registers.A);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x98: // SBC A,B
                        {
                            var toSubstract = (byte)(Registers.B + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x99: // SBC A,C
                        {
                            var toSubstract = (byte)(Registers.C + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x9A: // SBC A,D
                        {
                            var toSubstract = (byte)(Registers.D + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x9B: // SBC A,E
                        {
                            var toSubstract = (byte)(Registers.E + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x9C: // SBC A,H
                        {
                            var toSubstract = (byte)(Registers.H + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x9D: // SBC A,L
                        {
                            var toSubstract = (byte)(Registers.L + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0x9E: // SBC A,(HL)
                        {
                            var toSubstract = (byte)(memory[Registers.HL] - Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0x9F: // SBC A,A
                        {
                            var toSubstract = (byte)(Registers.A + Registers.C);
                            var temp = (byte)(Registers.A - toSubstract);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, toSubstract);
                            Registers.CFlag = !HasBorrow(Registers.A, toSubstract);

                            Registers.A = temp;

                            cycleCount += 4;
                        }
                        break;
                    case 0xA0: // AND A,B
                        Registers.A = (byte)(Registers.B & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA1: // AND A,C
                        Registers.A = (byte)(Registers.C & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA2: // AND A,D
                        Registers.A = (byte)(Registers.D & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA3: // AND A,E
                        Registers.A = (byte)(Registers.E & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA4: // AND A,H
                        Registers.A = (byte)(Registers.H & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA5: // AND A,L
                        Registers.A = (byte)(Registers.L & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA6: // AND A,(HL)
                        Registers.A = (byte)(memory[Registers.HL] & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 8;
                        break;
                    case 0xA7: // AND A,A
                        Registers.A = (byte)(Registers.A & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA8: // XOR B
                        Registers.A = (byte)(Registers.A ^ Registers.B);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xA9: // XOR C
                        Registers.A = (byte)(Registers.A ^ Registers.C);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAA: // XOR D
                        Registers.A = (byte)(Registers.A ^ Registers.D);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAB: // XOR E
                        Registers.A = (byte)(Registers.A ^ Registers.E);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAC: // XOR H
                        Registers.A = (byte)(Registers.A ^ Registers.H);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAD: // XOR L
                        Registers.A = (byte)(Registers.A ^ Registers.L);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAE: // XOR (HL)
                        Registers.A = (byte)(Registers.A ^ memory[Registers.HL]);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xAF: // XOR A
                        Registers.A = (byte)(Registers.A ^ Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB0: // OR A,B
                        Registers.A = (byte)(Registers.B | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB1: // OR A,C
                        Registers.A = (byte)(Registers.C | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB2: // OR A,D
                        Registers.A = (byte)(Registers.D | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB3: // OR A,E
                        Registers.A = (byte)(Registers.E | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB4: // OR A,H
                        Registers.A = (byte)(Registers.H | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB5: // OR A,L
                        Registers.A = (byte)(Registers.L | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB6: // OR A,(HL)
                        Registers.A = (byte)(memory[Registers.HL] | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 8;
                        break;
                    case 0xB7: // OR A,A
                        Registers.A = (byte)(Registers.A | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xB8: // CP B
                        {
                            Registers.ZFlag = Registers.A == Registers.B;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.B);
                            Registers.CFlag = Registers.A < Registers.B;

                            cycleCount += 4;
                        }
                        break;
                    case 0xB9: // CP C
                        {
                            Registers.ZFlag = Registers.A == Registers.C;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.C);
                            Registers.CFlag = Registers.A < Registers.C;

                            cycleCount += 4;
                        }
                        break;
                    case 0xBA: // CP D
                        {
                            Registers.ZFlag = Registers.A == Registers.D;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.D);
                            Registers.CFlag = Registers.A < Registers.D;

                            cycleCount += 4;
                        }
                        break;
                    case 0xBB: // CP E
                        {
                            Registers.ZFlag = Registers.A == Registers.E;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.E);
                            Registers.CFlag = Registers.A < Registers.E;

                            cycleCount += 4;
                        }
                        break;
                    case 0xBC: // CP H
                        {
                            Registers.ZFlag = Registers.A == Registers.H;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.H);
                            Registers.CFlag = Registers.A < Registers.H;

                            cycleCount += 4;
                        }
                        break;
                    case 0xBD: // CP L
                        {
                            Registers.ZFlag = Registers.A == Registers.L;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.L);
                            Registers.CFlag = Registers.A < Registers.L;

                            cycleCount += 4;
                        }
                        break;
                    case 0xBE: // CP (HL)
                        {
                            var temp = memory[Registers.HL];

                            Registers.ZFlag = Registers.A == temp;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, temp);
                            Registers.CFlag = Registers.A < temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0xBF: // CP A
                        {
                            Registers.ZFlag = Registers.A == Registers.A;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, Registers.A);
                            Registers.CFlag = Registers.A < Registers.A;

                            cycleCount += 4;
                        }
                        break;
                    case 0xC0: // RET NZ
                        {
                            if (!Registers.ZFlag)
                            {
                                RET();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0xC1: // POP BC
                        {
                            Registers.B = memory[Registers.SP++];
                            Registers.C = memory[Registers.SP++];
                            cycleCount += 12;
                        }

                        break;
                    case 0xC2: // JP NZ, nn
                        {
                            if (!Registers.ZFlag)
                            {
                                Registers.PC = GetUShortAtProgramCounter();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xC3: // JP nn
                        {
                            Registers.PC = GetUShortAtProgramCounter();

                            cycleCount += 12;
                        }
                        break;
                    case 0xC4: // CALL NZ,nn
                        {
                            if (!Registers.ZFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xC5: // PUSH BC
                        {
                            memory[--Registers.SP] = Registers.B;
                            memory[--Registers.SP] = Registers.C;
                            cycleCount += 16;
                        }
                        break;
                    case 0xC6: // ADD A,n
                        {
                            var regValue = memory[Registers.PC++];
                            var temp = (byte)(Registers.A + regValue);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, regValue);
                            Registers.CFlag = HasCarry(Registers.A, regValue);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0xC7: // RST 00H
                        {
                            RST_n(0x00);
                            cycleCount += 32;
                        }
                        break;
                    case 0xC8: // RET Z
                        {
                            if (Registers.ZFlag)
                            {
                                RET();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0xC9: // RET
                        {
                            RET();

                            cycleCount += 8;
                        }
                        break;
                    case 0xCA: // JP Z, nn
                        {
                            if (Registers.ZFlag)
                            {
                                Registers.PC = GetUShortAtProgramCounter();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xCB:
                        {
                            var nextOpCode = GetByteAtProgramCounter();

                            switch (nextOpCode)
                            {
                                case 0x00: // RLC B
                                    {
                                        var bit7 = (Registers.B & (1 << 7)) != 0;

                                        Registers.B = (byte)((Registers.B << 1) | (Registers.B >> 7));

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x01: // RLC C
                                    {
                                        var bit7 = (Registers.C & (1 << 7)) != 0;

                                        Registers.C = (byte)((Registers.C << 1) | (Registers.C >> 7));

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x02: // RLC D
                                    {
                                        var bit7 = (Registers.D & (1 << 7)) != 0;

                                        Registers.C = (byte)((Registers.D << 1) | (Registers.D >> 7));

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x03: // RLC E
                                    {
                                        var bit7 = (Registers.E & (1 << 7)) != 0;

                                        Registers.C = (byte)((Registers.E << 1) | (Registers.E >> 7));

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x04: // RLC H
                                    {
                                        var bit7 = (Registers.H & (1 << 7)) != 0;

                                        Registers.C = (byte)((Registers.H << 1) | (Registers.H >> 7));

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x05: // RLC L
                                    {
                                        var bit7 = (Registers.L & (1 << 7)) != 0;

                                        Registers.C = (byte)((Registers.L << 1) | (Registers.L >> 7));

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x06: // RLC (HL)
                                    {
                                        var memValue = memory[Registers.HL];
                                        var bit7 = (memValue & (1 << 7)) != 0;

                                        memory[Registers.HL] = (byte)((memValue << 1) | (memValue >> 7));

                                        Registers.ZFlag = memValue == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x07: // RLC A
                                    {
                                        var bit7 = (Registers.A & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.A << 1) | (Registers.A >> 7));

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x08: // RRC B
                                    {
                                        var bit0 = (Registers.B & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.B >> 1) | (Registers.B << 7));

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x09: // RRC C
                                    {
                                        var bit0 = (Registers.C & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.C >> 1) | (Registers.C << 7));

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x0A: // RRC D
                                    {
                                        var bit0 = (Registers.D & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.D >> 1) | (Registers.D << 7));

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x0B: // RRC E
                                    {
                                        var bit0 = (Registers.E & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.E >> 1) | (Registers.E << 7));

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x0C: // RRC H
                                    {
                                        var bit0 = (Registers.H & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.H >> 1) | (Registers.H << 7));

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x0D: // RRC L
                                    {
                                        var bit0 = (Registers.L & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.L >> 1) | (Registers.L << 7));

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x0E: // RRC (HL)
                                    {
                                        var memValue = memory[Registers.HL];
                                        var bit0 = (memValue & (1 << 0)) != 0;

                                        memory[Registers.HL] = (byte)((memValue >> 1) | (memValue << 7));

                                        Registers.ZFlag = memValue == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x0F: // RRC A
                                    {
                                        var bit0 = (Registers.A & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.A >> 1) | (Registers.A << 7));

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x10: // RL B
                                    {
                                        var bit7 = (Registers.B & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.B << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x11: // RL C
                                    {
                                        var bit7 = (Registers.C & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.C << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x12: // RL D
                                    {
                                        var bit7 = (Registers.D & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.D << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x13: // RL E
                                    {
                                        var bit7 = (Registers.E & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.E << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x14: // RL H
                                    {
                                        var bit7 = (Registers.H & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.H << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x15: // RL L
                                    {
                                        var bit7 = (Registers.L & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.L << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x16: // RL (HL)
                                    {
                                        var memValue = memory[Registers.HL];
                                        var bit7 = (memValue & (1 << 7)) != 0;

                                        memory[Registers.HL] = (byte)((memValue << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = memValue == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x17: // RL A
                                    {
                                        var bit7 = (Registers.A & (1 << 7)) != 0;

                                        Registers.A = (byte)((Registers.A << 1) | (Registers.CFlag ? 0x01 : 0x00));

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x18: // RR B
                                    {
                                        var bit0 = (Registers.B & (1 << 0)) != 0;

                                        Registers.B = (byte)((Registers.B >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x19: // RR C
                                    {
                                        var bit0 = (Registers.C & (1 << 0)) != 0;

                                        Registers.C = (byte)((Registers.C >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x1A: // RR D
                                    {
                                        var bit0 = (Registers.D & (1 << 0)) != 0;

                                        Registers.D = (byte)((Registers.D >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x1B: // RR E
                                    {
                                        var bit0 = (Registers.E & (1 << 0)) != 0;

                                        Registers.E = (byte)((Registers.E >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x1C: // RR H
                                    {
                                        var bit0 = (Registers.H & (1 << 0)) != 0;

                                        Registers.H = (byte)((Registers.H >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x1D: // RR L
                                    {
                                        var bit0 = (Registers.L & (1 << 0)) != 0;

                                        Registers.L = (byte)((Registers.L >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x1E: // RR (HL)
                                    {
                                        var memValue = memory[Registers.HL];
                                        var bit0 = (memValue & (1 << 0)) != 0;

                                        memory[Registers.HL] = (byte)((memValue >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = memValue == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x1F: // RR A
                                    {
                                        var bit0 = (Registers.A & (1 << 0)) != 0;

                                        Registers.A = (byte)((Registers.A >> 1) | (Registers.CFlag ? 0x80 : 0x00));

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x20: // SLA B
                                    {
                                        var bit7 = (Registers.B & (1 << 7)) != 0;

                                        Registers.B <<= 1;

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x21: // SLA C
                                    {
                                        var bit7 = (Registers.C & (1 << 7)) != 0;

                                        Registers.C <<= 1;

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x22: // SLA D
                                    {
                                        var bit7 = (Registers.D & (1 << 7)) != 0;

                                        Registers.D <<= 1;

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x23: // SLA E
                                    {
                                        var bit7 = (Registers.E & (1 << 7)) != 0;

                                        Registers.E <<= 1;

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x24: // SLA H
                                    {
                                        var bit7 = (Registers.H & (1 << 7)) != 0;

                                        Registers.H <<= 1;

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x25: // SLA L
                                    {
                                        var bit7 = (Registers.L & (1 << 7)) != 0;

                                        Registers.L <<= 1;

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x26: // SLA (HL)
                                    {
                                        var bit7 = (memory[Registers.HL] & (1 << 7)) != 0;

                                        memory[Registers.HL] <<= 1;

                                        Registers.ZFlag = memory[Registers.HL] == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x27: // SLA A
                                    {
                                        var bit7 = (Registers.A & (1 << 7)) != 0;

                                        Registers.A <<= 1;

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit7;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x28: // SRA B
                                    {
                                        var bit0 = (Registers.B & (1 << 0)) != 0;
                                        var bit7 = (Registers.B & (1 << 7));

                                        Registers.B = (byte)(bit7 | Registers.B >> 1);

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x29: // SRA C
                                    {
                                        var bit0 = (Registers.C & (1 << 0)) != 0;
                                        var bit7 = (Registers.C & (1 << 7));

                                        Registers.C = (byte)(bit7 | Registers.C >> 1);

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x2A: // SRA D
                                    {
                                        var bit0 = (Registers.D & (1 << 0)) != 0;
                                        var bit7 = (Registers.D & (1 << 7));

                                        Registers.D = (byte)(bit7 | Registers.D >> 1);

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x2B: // SRA E
                                    {
                                        var bit0 = (Registers.E & (1 << 0)) != 0;
                                        var bit7 = (Registers.E & (1 << 7));

                                        Registers.E = (byte)(bit7 | Registers.E >> 1);

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x2C: // SRA H
                                    {
                                        var bit0 = (Registers.H & (1 << 0)) != 0;
                                        var bit7 = (Registers.H & (1 << 7));

                                        Registers.H = (byte)(bit7 | Registers.H >> 1);

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x2D: // SRA L
                                    {
                                        var bit0 = (Registers.L & (1 << 0)) != 0;
                                        var bit7 = (Registers.L & (1 << 7));

                                        Registers.L = (byte)(bit7 | Registers.L >> 1);

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x2E: // SRA (HL)
                                    {
                                        var bit0 = (memory[Registers.HL] & (1 << 0)) != 0;
                                        var bit7 = (memory[Registers.HL] & (1 << 7));

                                        memory[Registers.HL] = (byte)(bit7 | memory[Registers.HL] >> 1);

                                        Registers.ZFlag = memory[Registers.HL] == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x2F: // SRA A
                                    {
                                        var bit0 = (Registers.A & (1 << 0)) != 0;
                                        var bit7 = (Registers.A & (1 << 7));

                                        Registers.A = (byte)(bit7 | Registers.A >> 1);

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x30: // SWAP B
                                    {
                                        Registers.B = (byte)((Registers.B & 0x0F) << 4 | (Registers.B & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x31: // SWAP C
                                    {
                                        Registers.C = (byte)((Registers.C & 0x0F) << 4 | (Registers.C & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x32: // SWAP D
                                    {
                                        Registers.D = (byte)((Registers.D & 0x0F) << 4 | (Registers.D & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x33: // SWAP E
                                    {
                                        Registers.E = (byte)((Registers.E & 0x0F) << 4 | (Registers.E & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x34: // SWAP H
                                    {
                                        Registers.H = (byte)((Registers.H & 0x0F) << 4 | (Registers.H & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x35: // SWAP L
                                    {
                                        Registers.L = (byte)((Registers.L & 0x0F) << 4 | (Registers.L & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x36: // SWAP (HL)
                                    {
                                        memory[Registers.HL] = (byte)((memory[Registers.HL] & 0x0F) << 4 | (memory[Registers.HL] & 0xF0) >> 4);

                                        Registers.ZFlag = memory[Registers.HL] == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x37: // SWAP A
                                    {
                                        Registers.A = (byte)((Registers.A & 0x0F) << 4 | (Registers.A & 0xF0) >> 4);

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = false;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x38: // SRL B
                                    {
                                        var bit0 = (Registers.B & (1 << 0)) != 0;

                                        Registers.B >>= 1;

                                        Registers.ZFlag = Registers.B == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x39: // SRL C
                                    {
                                        var bit0 = (Registers.C & (1 << 0)) != 0;

                                        Registers.C >>= 1;

                                        Registers.ZFlag = Registers.C == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x3A: // SRL D
                                    {
                                        var bit0 = (Registers.D & (1 << 0)) != 0;

                                        Registers.D >>= 1;

                                        Registers.ZFlag = Registers.D == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x3B: // SRL E
                                    {
                                        var bit0 = (Registers.E & (1 << 0)) != 0;

                                        Registers.E >>= 1;

                                        Registers.ZFlag = Registers.E == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x3C: // SRL H
                                    {
                                        var bit0 = (Registers.H & (1 << 0)) != 0;

                                        Registers.H >>= 1;

                                        Registers.ZFlag = Registers.H == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x3D: // SRL L
                                    {
                                        var bit0 = (Registers.L & (1 << 0)) != 0;

                                        Registers.L >>= 1;

                                        Registers.ZFlag = Registers.L == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x3E: // SRL (HL)
                                    {
                                        var bit0 = (memory[Registers.HL] & (1 << 0)) != 0;

                                        memory[Registers.HL] >>= 1;

                                        Registers.ZFlag = memory[Registers.HL] == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x3F: // SRL A
                                    {
                                        var bit0 = (Registers.A & (1 << 0)) != 0;

                                        Registers.A >>= 1;

                                        Registers.ZFlag = Registers.A == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = false;
                                        Registers.CFlag = bit0;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x40: // BIT 0, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x41: // BIT 0, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x42: // BIT 0, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x43: // BIT 0, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x44: // BIT 0, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x45: // BIT 0, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x46: // BIT 0, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x47: // BIT 0, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 0)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x48: // BIT 1, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x49: // BIT 1, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x4A: // BIT 1, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x4B: // BIT 1, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x4C: // BIT 1, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x4D: // BIT 1, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x4E: // BIT 1, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x4F: // BIT 1, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 1)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x50: // BIT 2, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x51: // BIT 2, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x52: // BIT 2, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x53: // BIT 2, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x54: // BIT 2, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x55: // BIT 2, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x56: // BIT 2, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x57: // BIT 2, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 2)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x58: // BIT 3, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x59: // BIT 3, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x5A: // BIT 3, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x5B: // BIT 3, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x5C: // BIT 3, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x5D: // BIT 3, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x5E: // BIT 3, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x5F: // BIT 3, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 3)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x60: // BIT 4, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x61: // BIT 4, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x62: // BIT 4, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x63: // BIT 4, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x64: // BIT 4, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x65: // BIT 4, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x66: // BIT 4, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x67: // BIT 4, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 4)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x68: // BIT 5, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x69: // BIT 5, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x6A: // BIT 5, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x6B: // BIT 5, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x6C: // BIT 5, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x6D: // BIT 5, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x6E: // BIT 5, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x6F: // BIT 5, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 5)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x70: // BIT 6, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x71: // BIT 6, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x72: // BIT 6, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x73: // BIT 6, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x74: // BIT 6, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x75: // BIT 6, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x76: // BIT 6, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x77: // BIT 6, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 6)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x78: // BIT 7, B
                                    {
                                        Registers.ZFlag = (Registers.B & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x79: // BIT 7, C
                                    {
                                        Registers.ZFlag = (Registers.C & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x7A: // BIT 7, D
                                    {
                                        Registers.ZFlag = (Registers.D & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x7B: // BIT 7, E
                                    {
                                        Registers.ZFlag = (Registers.E & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x7C: // BIT 7, H
                                    {
                                        Registers.ZFlag = (Registers.H & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x7D: // BIT 7, L
                                    {
                                        Registers.ZFlag = (Registers.L & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x7E: // BIT 7, (HL)
                                    {
                                        Registers.ZFlag = (memory[Registers.HL] & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x7F: // BIT 7, A
                                    {
                                        Registers.ZFlag = (Registers.A & (1 << 7)) == 0;
                                        Registers.NFlag = false;
                                        Registers.HFlag = true;

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x80: // RES 0, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x81: // RES 0, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x82: // RES 0, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x83: // RES 0, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x84: // RES 0, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x85: // RES 0, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x86: // RES 0, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 0));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x87: // RES 0, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 0));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x88: // RES 1, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x89: // RES 1, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x8A: // RES 1, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x8B: // RES 1, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x8C: // RES 1, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x8D: // RES 1, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x8E: // RES 1, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 1));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x8F: // RES 1, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 1));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x90: // RES 2, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x91: // RES 2, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x92: // RES 2, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x93: // RES 2, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x94: // RES 2, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x95: // RES 2, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x96: // RES 2, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 2));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x97: // RES 2, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 2));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x98: // RES 3, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x99: // RES 3, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x9A: // RES 3, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x9B: // RES 3, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x9C: // RES 3, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x9D: // RES 3, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0x9E: // RES 3, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 3));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0x9F: // RES 3, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 3));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA0: // RES 4, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA1: // RES 4, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA2: // RES 4, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA3: // RES 4, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA4: // RES 4, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA5: // RES 4, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA6: // RES 4, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 4));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xA7: // RES 4, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 4));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA8: // RES 5, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xA9: // RES 5, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xAA: // RES 5, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xAB: // RES 5, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xAC: // RES 5, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xAD: // RES 5, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xAE: // RES 5, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 5));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xAF: // RES 5, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 5));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB0: // RES 6, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB1: // RES 6, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB2: // RES 6, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB3: // RES 6, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB4: // RES 6, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB5: // RES 6, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB6: // RES 6, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 6));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xB7: // RES 6, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 6));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB8: // RES 7, B
                                    {
                                        Registers.B = (byte)(Registers.B & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xB9: // RES 7, C
                                    {
                                        Registers.C = (byte)(Registers.C & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xBA: // RES 7, D
                                    {
                                        Registers.D = (byte)(Registers.D & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xBB: // RES 7, E
                                    {
                                        Registers.E = (byte)(Registers.E & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xBC: // RES 7, H
                                    {
                                        Registers.H = (byte)(Registers.H & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xBD: // RES 7, L
                                    {
                                        Registers.L = (byte)(Registers.L & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xBE: // RES 7, (HL)
                                    {
                                        memory[Registers.HL] = (byte)(memory[Registers.HL] & ~(1 << 7));

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xBF: // RES 7, A
                                    {
                                        Registers.A = (byte)(Registers.A & ~(1 << 7));

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC0: // SET 0, B
                                    {
                                        Registers.B |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC1: // SET 0, C
                                    {
                                        Registers.C |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC2: // SET 0, D
                                    {
                                        Registers.D |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC3: // SET 0, E
                                    {
                                        Registers.E |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC4: // SET 0, H
                                    {
                                        Registers.H |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC5: // SET 0, L
                                    {
                                        Registers.L |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC6: // SET 0, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 0);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xC7: // SET 0, A
                                    {
                                        Registers.A |= (1 << 0);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC8: // SET 1, B
                                    {
                                        Registers.B |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xC9: // SET 1, C
                                    {
                                        Registers.C |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xCA: // SET 1, D
                                    {
                                        Registers.D |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xCB: // SET 1, E
                                    {
                                        Registers.E |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xCC: // SET 1, H
                                    {
                                        Registers.H |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xCD: // SET 1, L
                                    {
                                        Registers.L |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xCE: // SET 1, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 1);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xCF: // SET 1, A
                                    {
                                        Registers.A |= (1 << 1);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD0: // SET 2, B
                                    {
                                        Registers.B |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD1: // SET 2, C
                                    {
                                        Registers.C |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD2: // SET 2, D
                                    {
                                        Registers.D |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD3: // SET 2, E
                                    {
                                        Registers.E |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD4: // SET 2, H
                                    {
                                        Registers.H |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD5: // SET 2, L
                                    {
                                        Registers.L |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD6: // SET 2, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 2);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xD7: // SET 2, A
                                    {
                                        Registers.A |= (1 << 2);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD8: // SET 3, B
                                    {
                                        Registers.B |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xD9: // SET 3, C
                                    {
                                        Registers.C |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xDA: // SET 3, D
                                    {
                                        Registers.D |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xDB: // SET 3, E
                                    {
                                        Registers.E |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xDC: // SET 3, H
                                    {
                                        Registers.H |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xDD: // SET 3, L
                                    {
                                        Registers.L |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xDE: // SET 3, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 3);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xDF: // SET 3, A
                                    {
                                        Registers.A |= (1 << 3);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE0: // SET 4, B
                                    {
                                        Registers.B |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE1: // SET 4, C
                                    {
                                        Registers.C |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE2: // SET 4, D
                                    {
                                        Registers.D |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE3: // SET 4, E
                                    {
                                        Registers.E |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE4: // SET 4, H
                                    {
                                        Registers.H |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE5: // SET 4, L
                                    {
                                        Registers.L |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE6: // SET 4, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 4);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xE7: // SET 4, A
                                    {
                                        Registers.A |= (1 << 4);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE8: // SET 5, B
                                    {
                                        Registers.B |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xE9: // SET 5, C
                                    {
                                        Registers.C |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xEA: // SET 5, D
                                    {
                                        Registers.D |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xEB: // SET 5, E
                                    {
                                        Registers.E |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xEC: // SET 5, H
                                    {
                                        Registers.H |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xED: // SET 5, L
                                    {
                                        Registers.L |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xEE: // SET 5, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 5);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xEF: // SET 5, A
                                    {
                                        Registers.A |= (1 << 5);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF0: // SET 6, B
                                    {
                                        Registers.B |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF1: // SET 6, C
                                    {
                                        Registers.C |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF2: // SET 6, D
                                    {
                                        Registers.D |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF3: // SET 6, E
                                    {
                                        Registers.E |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF4: // SET 6, H
                                    {
                                        Registers.H |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF5: // SET 6, L
                                    {
                                        Registers.L |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF6: // SET 6, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 6);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xF7: // SET 6, A
                                    {
                                        Registers.A |= (1 << 6);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF8: // SET 7, B
                                    {
                                        Registers.B |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xF9: // SET 7, C
                                    {
                                        Registers.C |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xFA: // SET 7, D
                                    {
                                        Registers.D |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xFB: // SET 7, E
                                    {
                                        Registers.E |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xFC: // SET 7, H
                                    {
                                        Registers.H |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xFD: // SET 7, L
                                    {
                                        Registers.L |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                                case 0xFE: // SET 7, (HL)
                                    {
                                        memory[Registers.HL] |= (1 << 7);

                                        cycleCount += 16;
                                    }
                                    break;
                                case 0xFF: // SET 7, A
                                    {
                                        Registers.A |= (1 << 7);

                                        cycleCount += 8;
                                    }
                                    break;
                            }
                        }
                        break;
                    case 0xCC: // CALL Z,nn
                        {
                            if (Registers.ZFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xCD: // CALL nn
                        {
                            CALL_nn();

                            cycleCount += 12;
                        }
                        break;
                    case 0xCE: // ADC A,#
                        {
                            var regValue = GetByteAtProgramCounter();
                            var temp = (byte)(Registers.A + regValue + Registers.C);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = false;
                            Registers.HFlag = HasHalfCarry(Registers.A, regValue, Registers.C);
                            Registers.CFlag = HasCarry(temp);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0xCF: // RST 08H
                        {
                            RST_n(0x08);
                            cycleCount += 32;
                        }
                        break;
                    case 0xD0: // RET NC
                        {
                            if (!Registers.CFlag)
                            {
                                RET();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0xD1: // POP DE
                        Registers.D = memory[Registers.SP++];
                        Registers.E = memory[Registers.SP++];
                        cycleCount += 12;
                        break;
                    case 0xD2: // JP NC, nn
                        {
                            if (!Registers.CFlag)
                            {
                                Registers.PC = GetUShortAtProgramCounter();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xD4: // CALL NC,nn
                        {
                            if (!Registers.CFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xD5: // PUSH DE
                        memory[--Registers.SP] = Registers.D;
                        memory[--Registers.SP] = Registers.E;
                        cycleCount += 16;
                        break;
                    case 0xD6: // SUB A,#
                        {
                            var regValue = GetByteAtProgramCounter();
                            var temp = (byte)(Registers.A - regValue);

                            Registers.ZFlag = temp == 0;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, regValue);
                            Registers.CFlag = !HasBorrow(Registers.A, regValue);

                            Registers.A = temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0xD7: // RST 10H
                        {
                            RST_n(0x10);
                            cycleCount += 32;
                        }
                        break;
                    case 0xD8: // RET C
                        {
                            if (Registers.CFlag)
                            {
                                RET();
                            }
                            cycleCount += 8;
                        }
                        break;
                    case 0xD9: // RETI
                        {
                            RET();
                            EI();
                            
                            cycleCount += 8;
                        }
                        break;
                    case 0xDA: // JP C, nn
                        {
                            if (!Registers.CFlag)
                            {
                                Registers.PC = GetUShortAtProgramCounter();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xDC: // CALL C,nn
                        {
                            if (Registers.CFlag)
                            {
                                CALL_nn();
                            }

                            cycleCount += 12;
                        }
                        break;
                    case 0xDF: // RST 18H
                        {
                            RST_n(0x18);
                            cycleCount += 32;
                        }
                        break;
                    case 0xE0: // LDH (n),A
                        memory[0xFF00 + GetByteAtProgramCounter()] = Registers.A;
                        cycleCount += 12;
                        break;
                    case 0xE1: // POP HL
                        Registers.H = memory[Registers.SP++];
                        Registers.L = memory[Registers.SP++];
                        cycleCount += 12;
                        break;
                    case 0xE2: // LD (C),A
                        memory[0xFF00 + Registers.C] = Registers.A;
                        cycleCount += 8;
                        break;
                    case 0xE5: // PUSH HL
                        memory[--Registers.SP] = Registers.H;
                        memory[--Registers.SP] = Registers.L;
                        cycleCount += 16;
                        break;
                    case 0xE6: // AND A,#
                        Registers.A = (byte)(GetByteAtProgramCounter() & Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = true;
                        Registers.CFlag = false;

                        cycleCount += 8;
                        break;
                    case 0xE7: // RST 20H
                        {
                            RST_n(0x20);
                            cycleCount += 32;
                        }
                        break;
                    case 0xE8: // ADD SP,n
                        {
                            var value = GetByteAtProgramCounter();

                            Registers.ZFlag = false;
                            Registers.NFlag = false;
                            Registers.CFlag = HasCarry(Registers.SP, value);
                            Registers.HFlag = HasHalfCarry(Registers.SP, value);

                            Registers.SP += value;

                            cycleCount += 16; // TODO: check if this should be 8 instead
                        }
                        break;
                    case 0xE9: // JP (HL)
                        {
                            Registers.PC = memory[Registers.HL];
                            cycleCount += 4;
                        }
                        break;
                    case 0xEA: // LD (NN),A
                        memory[GetUShortAtProgramCounter()] = Registers.A;
                        cycleCount += 16;
                        break;
                    case 0xEE: // XOR #
                        Registers.A = (byte)(Registers.A ^ GetByteAtProgramCounter());

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 4;
                        break;
                    case 0xEF: // RST 28H
                        {
                            RST_n(0x28);
                            cycleCount += 32;
                        }
                        break;
                    case 0xF0: // LDH A,(n)
                        Registers.A = memory[0xFF00 + GetByteAtProgramCounter()];
                        cycleCount += 12;
                        break;
                    case 0xF1: // POP AF
                        Registers.A = memory[Registers.SP++];
                        Registers.F = memory[Registers.SP++];
                        cycleCount += 12;
                        break;
                    case 0xF2: // LD A,(C)
                        Registers.A = memory[0xFF00 + Registers.C];
                        cycleCount += 8;
                        break;
                    case 0xF3: // DI
                        mustDisableInterrupts = true;
                        cycleCount += 4;
                        break;
                    case 0xF5: // PUSH AF
                        memory[--Registers.SP] = Registers.A;
                        memory[--Registers.SP] = Registers.F;
                        cycleCount += 16;
                        break;
                    case 0xF6: // OR A,#
                        Registers.A = (byte)(GetByteAtProgramCounter() | Registers.A);

                        Registers.ZFlag = Registers.A == 0;
                        Registers.NFlag = false;
                        Registers.HFlag = false;
                        Registers.CFlag = false;

                        cycleCount += 8;
                        break;
                    case 0xF7: // RST 30H
                        {
                            RST_n(0x30);
                            cycleCount += 32;
                        }
                        break;
                    case 0xF8: // LD HL,SP+n / LDHL SP,n
                        var n = memory[GetByteAtProgramCounter()];

                        Registers.HL = (ushort)(Registers.SP + n);

                        Registers.ZFlag = false;
                        Registers.NFlag = false;
                        Registers.HFlag = HasHalfCarry(Registers.SP, n);
                        Registers.CFlag = HasCarry(Registers.SP, n);

                        cycleCount += 12;
                        break;
                    case 0xF9: // LD SP,HL
                        Registers.SP = Registers.HL;
                        cycleCount += 8;
                        break;
                    case 0xFA: // LD A,(NN)
                        Registers.A = memory[GetUShortAtProgramCounter()];
                        cycleCount += 16;
                        break;
                    case 0xFB: // EI
                        {
                            EI();
                            cycleCount += 4;
                        }
                        break;
                    case 0xFE: // CP #
                        {
                            var temp = GetByteAtProgramCounter();

                            Registers.ZFlag = Registers.A == temp;
                            Registers.NFlag = true;
                            Registers.HFlag = !HasHalfBorrow(Registers.A, temp);
                            Registers.CFlag = Registers.A < temp;

                            cycleCount += 8;
                        }
                        break;
                    case 0xFF: // RST 38H
                        {
                            RST_n(0x38);
                            cycleCount += 32;
                        }
                        break;
                }

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
            } while ( cycleCount <= 70224 );
        }

        private void EI()
        {
            mustEnableInterrupts = true;
        }

        private void RET()
        {
            var nextOpcode = ( ushort ) ( ( memory[ Registers.SP++ ] ) | ( memory[ Registers.SP++ ] << 8 ) );

            Registers.PC = nextOpcode;
        }

        private void RST_n( ushort offset )
        {
            memory[ --Registers.SP ] = ( byte ) ( ( Registers.PC & 0xFF00 ) >> 8 );
            memory[ --Registers.SP ] = ( byte ) ( ( Registers.PC & 0X00FF ) );

            Registers.PC = ( ushort ) ( 0x0000 + offset );
        }

        private void CALL_nn()
        {
            memory[ --Registers.SP ] = ( byte ) ( ( ( Registers.PC + 3 ) & 0xFF00 ) >> 8 );
            memory[ --Registers.SP ] = ( byte ) ( ( ( Registers.PC + 3 ) & 0X00FF ) );

            var nextOpCode = GetUShortAtProgramCounter();
            Registers.PC = nextOpCode;
        }

        public void Reset()
        {
            cycleCount = 0;
            Registers.Reset();
        }

        private byte GetByteAtProgramCounter()
        {
            return memory[ Registers.PC++ ];
        }

        private ushort GetUShortAtProgramCounter()
        {
            var lowOrder = memory[ Registers.PC++ ];
            var highOrder = memory[ Registers.PC++ ];

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
            memory[ Registers.PC++ ] = ( byte ) ( value );
            memory[ Registers.PC++ ] = ( byte ) ( value >> 8 );
        }

        private const int frameDuration = 70224;
        private int cycleCount;
        //private byte[] romData;
        private bool mustDisableInterrupts = false;
        private bool mustEnableInterrupts = false;
    }
}