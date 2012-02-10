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
					case 0x06: // LD B,n
						Registers.B = romData[ Registers.PC ];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x0E: // LD C,n
						Registers.C = romData[ Registers.PC ];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x16: // LD D,n
						Registers.D = romData[Registers.PC];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x1E: // LD E,n
						Registers.E = romData[Registers.PC];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x26: // LD H,n
						Registers.H = romData[Registers.PC];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x2E: // LD L,n
						Registers.L = romData[Registers.PC];
						Registers.PC++;
						cycleCount += 8;
						break;
					case 0x7F: // LD A,A
						Registers.A = Registers.A;
						cycleCount += 4;
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
						Registers.A = Registers.HL;
						cycleCount += 4;
						break;
				}

			} while ( cycleCount <= 70224 );
		}
		
		public CPURegisters Registers { get; set; }
		public Memory ROMMemory { get; private set; }

		private const int frameDuration = 70224;

		private int cycleCount;
		private byte[] romData;
	}
}
