using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class Emulator
    {
        private Processor processor;
        private Memory memory;
        
        public void Load( byte[] rom )
        {
            memory = new Memory( rom );
            
            processor = new Processor( memory );

            memory.Initialize();
            
            EmulateFrame();
        }

        public void EmulateFrame()
        {
            processor.EmulateFrame();
        }

        public void Reset()
        {
            processor.Reset();
        }

        public string GameName { get { return memory.GameName; } }
    }
}
