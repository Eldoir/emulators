namespace GameboyEmulator
{
    public class Memory
    {
        private readonly Cartridge cartridge;
        private readonly GPU gpu;
        private readonly CPURegisters cpuRegisters;
        private readonly GPURegisters gpuRegisters;

        //private readonly byte[] videoRAMData = new byte[0x2000];
        private readonly byte[] cartridgeExternalRAMData = new byte[0x2000];
        private readonly byte[] workingRAMData = new byte[0x2000];
        private readonly byte[] spriteData = new byte[0xA0];
        private readonly byte[] emptyButIOData = new byte[0x60];
        private readonly byte[] ioPortsData = new byte[0x4C];
        private readonly byte[] emptyButIOBisData = new byte[0x34];
        private readonly byte[] internalRAMBisData = new byte[0x7F];

        private byte interruptEnableRegister;
        private bool inBIOS = true;

        public Memory( Cartridge cartridge, GPU gpu, CPURegisters cpuRegisters, GPURegisters gpuRegisters )
        {
            this.cartridge = cartridge;
            this.gpu = gpu;
            this.cpuRegisters = cpuRegisters;
            this.gpuRegisters = gpuRegisters;
        }

        public void Initialize()
        {
            cartridge.Initialize();

            this[ 0xFF05 ] = 0x00; //TIMA
            this[ 0xFF06 ] = 0x00; //TMA
            this[ 0xFF07 ] = 0x00; //TAC
            this[ 0xFF10 ] = 0x80; //NR10
            this[ 0xFF11 ] = 0xBF; //NR11
            this[ 0xFF12 ] = 0xF3; //NR12
            this[ 0xFF14 ] = 0xBF; //NR14
            this[ 0xFF16 ] = 0x3F; //NR21
            this[ 0xFF17 ] = 0x00; //NR22
            this[ 0xFF19 ] = 0xBF; //NR24
            this[ 0xFF1A ] = 0x7F; //NR30
            this[ 0xFF1B ] = 0xFF; //NR31
            this[ 0xFF1C ] = 0x9F; //NR32
            this[ 0xFF1E ] = 0xBF; //NR33
            this[ 0xFF20 ] = 0xFF; //NR41
            this[ 0xFF21 ] = 0x00; //NR42
            this[ 0xFF22 ] = 0x00; //NR43
            this[ 0xFF23 ] = 0xBF; //NR30
            this[ 0xFF24 ] = 0x77; //NR50
            this[ 0xFF25 ] = 0xF3; //NR51
            
            if ( cartridge.GameBoyType == GameBoyType.SuperGameBoy )
            {
                this[0xFF26] = 0xF0; // NR52
            }
            else
            {
                this[0xFF26] = 0xF1; //NR52
            }

            this[0xFF40] = 0x91; //LCDC
            this[0xFF42] = 0x00; //SCY
            this[0xFF43] = 0x00; //SCX
            this[0xFF45] = 0x00; //LYC
            this[0xFF47] = 0xFC; //BGP
            this[0xFF48] = 0xFF; //OBP0
            this[0xFF49] = 0xFF; //OBP1
            this[0xFF4A] = 0x00; //WY
            this[0xFF4B] = 0x00; //WX
            this[ 0xFFFF ] = 0x00; //IE
        }

        public Cartridge Cartridge
        {
            get { return cartridge; }
        }

        public byte this[ushort offset]
        {
            get
            {
                switch (offset & 0xF000)
                {
                    // BIOS
                    case 0x0000:
                        if (inBIOS)
                        {
                            if (offset < 0x0100)
                                return cartridge[offset];
                            if( cpuRegisters.PC == 0x0100)
                                inBIOS = false;
                        }

                        return cartridge[offset];

                        // ROM 0
                    case 0x1000:
                    case 0x2000:
                    case 0x3000:
                            return cartridge[offset];

                    // ROM1 (unbanked) (16k)
                    case 0x4000:
                    case 0x5000:
                    case 0x6000:
                    case 0x7000:
                            return cartridge[offset];

                    // Graphics: VRAM (8k)
                    case 0x8000:
                    case 0x9000:
                        return gpu.ReadFromRAM( offset - 0x8000 );

                    // External RAM (8k)
                    case 0xA000:
                    case 0xB000:
                        return cartridgeExternalRAMData[offset - 0xA000];

                    // Working RAM (8k)
                    case 0xC000:
                    case 0xD000:
                        return workingRAMData[ offset - 0xC000 ];

                    // Working RAM shadow
                    case 0xE000:
                        return workingRAMData[offset - 0xE000];

                    // Working RAM shadow, I/O, Zero-page RAM
                    case 0xF000:
                        switch (offset & 0x0F00)
                        {
                            // Working RAM shadow
                            case 0x000:
                            case 0x100:
                            case 0x200:
                            case 0x300:
                            case 0x400:
                            case 0x500:
                            case 0x600:
                            case 0x700:
                            case 0x800:
                            case 0x900:
                            case 0xA00:
                            case 0xB00:
                            case 0xC00:
                            case 0xD00:
                                return workingRAMData[offset - 0xF000];

                            // Graphics: object attribute memory
                            // OAM is 160 bytes, remaining bytes read as 0
                            case 0xE00:
                                if (offset < 0xFEA0)
                                    return gpu.ReadFromOAM(offset - 0xFE00);
                                else
                                    return 0;

                            // Zero-page
                            case 0xF00:
                                if (offset >= 0xFF80 && offset < 0xFFFF )
                                {
                                    return gpu.ReadFromZeroPageRAM(offset - 0xFF80);
                                }
                                
                                // I/O control handling

                                switch ( offset & 0x00F0 )
                                {
                                    // GPU (64 registers)
                                    case 0x40:
                                    case 0x50:
                                    case 0x60:
                                    case 0x70:
                                        return gpuRegisters.Read( offset );
                                }
                                break;
                        }
                        break;
                }

                return interruptEnableRegister;
            }
            set
            {
                switch (offset & 0xF000)
                {
                    // BIOS
                    case 0x0000:
                        if (inBIOS)
                        {
                            if (offset < 0x0100)
                                cartridge[offset] = value;
                            if (cpuRegisters.PC == 0x0100)
                                inBIOS = false;
                        }

                        cartridge[offset] = value;
                        break;

                    // ROM 0
                    case 0x1000:
                    case 0x2000:
                    case 0x3000:
                        cartridge[offset] = value;
                        break;

                    // ROM1 (unbanked) (16k)
                    case 0x4000:
                    case 0x5000:
                    case 0x6000:
                    case 0x7000:
                        cartridge[offset] = value;
                        break;

                    // Graphics: VRAM (8k)
                    case 0x8000:
                    case 0x9000:
                        gpu.WriteInRAM( offset - 0x8000, value );
                        break;

                    // External RAM (8k)
                    case 0xA000:
                    case 0xB000:
                        cartridgeExternalRAMData[ offset - 0xA000 ] = value;
                        break;

                    // Working RAM (8k)
                    case 0xC000:
                    case 0xD000:
                        workingRAMData[offset - 0xC000] = value;
                        break;

                    // Working RAM shadow
                    case 0xE000:
                        workingRAMData[offset - 0xE000] = value;
                        break;

                    // Working RAM shadow, I/O, Zero-page RAM
                    case 0xF000:
                        switch (offset & 0x0F00)
                        {
                            // Working RAM shadow
                            case 0x000:
                            case 0x100:
                            case 0x200:
                            case 0x300:
                            case 0x400:
                            case 0x500:
                            case 0x600:
                            case 0x700:
                            case 0x800:
                            case 0x900:
                            case 0xA00:
                            case 0xB00:
                            case 0xC00:
                            case 0xD00:
                                workingRAMData[offset - 0xF000] = value;
                                break;

                            // Graphics: object attribute memory
                            // OAM is 160 bytes, remaining bytes read as 0
                            case 0xE00:
                                if (offset < 0xFEA0)
                                    gpu.WriteToOAM( offset - 0xFE00, value );
                                else
                                {    
                                }
                                break;
                            // Zero-page
                            case 0xF00:
                            {
                                if (offset >= 0xFF80 && offset < 0xFFFF)
                                {
                                    gpu.WriteToZeroPageRAM(offset - 0xFF80, value);
                                }
                                else
                                {
                                    // I/O control handling

                                    switch ( offset & 0x00F0 )
                                    {
                                        // GPU (64 registers)
                                        case 0x40:
                                        case 0x50:
                                        case 0x60:
                                        case 0x70:
                                            gpuRegisters.Write( offset, value );
                                            break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                }

                interruptEnableRegister = value;
            }
        }
    }
}
