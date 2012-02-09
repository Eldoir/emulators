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

                }

                cycleCount += 4;

            } while ( cycleCount <= 70224 );
        }
        
        public CPURegisters Registers { get; set; }
        public Memory ROMMemory { get; private set; }

        private const int frameDuration = 70224;

        private int cycleCount;
        private byte[] romData;
    }
}
