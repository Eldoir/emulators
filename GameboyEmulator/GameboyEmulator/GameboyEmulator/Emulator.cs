using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class Emulator
    {
        private Cartridge cartridge;
        private Memory memory;
        private Processor processor;
        private GPU gpu;
        private CPURegisters registers;
        private Clock clock;

        public void Load( byte[] rom )
        {
            cartridge = new Cartridge(rom); 
            
            clock = new Clock();
            
            registers = new CPURegisters( cartridge );
            gpu = new GPU( registers, clock );

            memory = new Memory( cartridge, gpu);

            processor = new Processor( memory, registers, gpu, clock );

            memory.Initialize();
            processor.Initialize();

            EmulateFrame();
        }

        public void EmulateFrame()
        {
            processor.EmulateFrame();
        }

        public string GameName { get { return cartridge.GameName; } }
    }
}
