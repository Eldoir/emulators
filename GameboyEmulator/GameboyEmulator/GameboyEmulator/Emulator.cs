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
        private CPURegisters cpuRegisters;
        private Clock clock;
        private GPURegisters gpuRegisters;

        public void Load( byte[] rom )
        {
            cartridge = new Cartridge(rom); 
            
            clock = new Clock();
            
            cpuRegisters = new CPURegisters( cartridge );
            gpuRegisters = new GPURegisters();

            gpu = new GPU( clock );

            memory = new Memory( cartridge, gpu, cpuRegisters, gpuRegisters );

            processor = new Processor( memory, cpuRegisters, gpu, clock );

            memory.Initialize();
            processor.Initialize();
        }

        public void EmulateFrame()
        {
            processor.EmulateFrame();
        }

        public string GameName { get { return cartridge.GameName; } }
    }
}
