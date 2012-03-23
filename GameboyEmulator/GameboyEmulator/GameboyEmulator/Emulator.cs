using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

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
        private Keyboard keyboard;

        public void Load( byte[] rom )
        {
            cartridge = new Cartridge(rom); 
            
            clock = new Clock();
            
            cpuRegisters = new CPURegisters( cartridge );
            gpuRegisters = new GPURegisters();

            gpu = new GPU( clock, gpuRegisters );

            keyboard = new Keyboard();

            memory = new Memory( cartridge, gpu, cpuRegisters, gpuRegisters, keyboard );

            processor = new Processor( memory, cpuRegisters, gpu, clock );

            memory.Initialize();
            processor.Initialize();
        }

        public void EmulateFrame()
        {
            processor.EmulateFrame();
        }

        public void KeyUp( Key key)
        {
            keyboard.KeyUp(key);
        }

        public void KeyDown(Key key)
        {
            keyboard.KeyDown(key);
        }

        public string GameName { get { return cartridge.GameName; } }
    }
}
