using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class CPURegisters
    {
        private readonly Cartridge cartridge;

        public CPURegisters( Cartridge cartridge )
        {
            this.cartridge = cartridge;
        }

        public void Initialize()
        {
            if ( cartridge.GameBoyType == GameBoyType.GameBoy
                || cartridge.GameBoyType == GameBoyType.SuperGameBoy )
            {
                AF = 0x01;
            }
            else if ( cartridge.GameBoyType == GameBoyType.GameBoyPocket )
            {
                AF = 0xFF;
            }
            else
            {
                AF = 0x11;
            }

            F = 0xB0;
            BC = 0x0013;
            DE = 0x00D8;
            HL = 0x014D;

            PC = 0x100;
            SP = 0xFFFE;
        }

        public byte A;
        public byte F;
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte H;
        public byte L;

        public ushort AF
        {
            get { return (ushort)((A << 8) | F); }
            set
            {
                A = (byte)((value >> 8) & 0xff);
                F = (byte)(value & 0xff);
            }
        }

        public ushort BC
        {
            get { return (ushort)((B << 8) | C); }
            set
            {
                B = (byte)((value >> 8) & 0xff);
                C = (byte)(value & 0xff);
            }
        }

        public ushort DE
        {
            get { return (ushort)((D << 8) | E); }
            set
            {
                D = (byte)((value >> 8) & 0xff);
                E = (byte)(value & 0xff);
            }
        }

        public ushort HL
        {
            get { return (ushort)((H << 8) | L); }
            set
            {
                H = (byte)((value >> 8) & 0xff);
                L = (byte)(value & 0xff);
            }
        }

        public ushort SP { get; set; }
        public ushort PC { get; set; }

        public bool ZFlag
        {
            get { return (F & zFlag) != 0; }
            set
            {
                if (value)
                {
                    F |= zFlag;
                }
                else
                {
                    F &= notZFlag;
                }
            }
        }

        public bool NFlag
        {
            get { return (F & nFlag) != 0; }
            set
            {
                if (value)
                {
                    F |= nFlag;
                }
                else
                {
                    F &= notNFlag;
                }
            }
        }

        public bool HFlag
        {
            get { return (F & hFlag) != 0; }
            set
            {
                if (value)
                {
                    F |= hFlag;
                }
                else
                {
                    F &= notHFlag;
                }
            }
        }

        public bool CFlag
        {
            get { return (F & cFlag) != 0; }
            set
            {
                if (value)
                {
                    F |= cFlag;
                }
                else
                {
                    F &= notCFlag;
                }
            }
        }

        // This bit becomes set (1) if the result of an operation has been zero (0)
        private const byte zFlag = 0x80;
        // N Indicates whether the previous instruction has been an addition or subtraction
        private const byte nFlag = 0x40;
        // H indicates carry for lower 4bits of the result
        private const byte hFlag = 0x20;
        /* Becomes set when the result of an addition became bigger than FFh (8bit) or FFFFh (16bit). 
         * Or when the result of a subtraction or comparison became less than zero (much as for Z80 and 80x86 CPUs, but unlike as for 65XX and ARM CPUs). 
         * Also the flag becomes set when a rotate/shift operation has shifted-out a "1"-bit. */
        private const byte cFlag = 0x10;

        // The following bytes are used to negate the bits sets by the above flags, on the F register
        private const byte notZFlag = 0x80;
        private const byte notNFlag = 0x40;
        private const byte notHFlag = 0x20;
        private const byte notCFlag = 0x10;
    }
}
