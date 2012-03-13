namespace GameboyEmulator
{
    public class Memory
    {
        private const int cartridgeMemoryEndOffset = 0x8000;
        private const int videoRAMEndOffset = 0xA000;
        private const int switchableRAMBankEndOffset = 0xC000;
        private const int internalRAMEndOffset = 0xE000;
        private const int echoInternalRAMEndOffset = 0xFE00;
        private const int spriteMemoryEndOffset = 0xFEA0;
        private const int emptyExceptIOEndOffset = 0xFF00;
        private const int ioPortsEndOffset = 0xFF4C;
        private const int emptyExceptIOBisEndOffset = 0xFF80;
        private const int internalRAMBisEndOffset = 0xFFFF;
        private const int interruptEnableRegisterOffset = 0xFFFF;

        private readonly Cartridge cartridge;
        private readonly GPU gpu;

        //private readonly byte[] videoRAMData = new byte[0x2000];
        private readonly byte[] switchableRAMBankData = new byte[0x2000];
        private readonly byte[] internalRAMData = new byte[0x2000];
        private readonly byte[] spriteData = new byte[0xA0];
        private readonly byte[] emptyButIOData = new byte[0x60];
        private readonly byte[] ioPortsData = new byte[0x4C];
        private readonly byte[] emptyButIOBisData = new byte[0x34];
        private readonly byte[] internalRAMBisData = new byte[0x7F];

        private byte interruptEnableRegister;

        public Memory( Cartridge cartridge, GPU gpu )
        {
            this.cartridge = cartridge;
            this.gpu = gpu;
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
            
            this[ 0xFF40 ] = 0x91; //LCDC
            this[ 0xFF42 ] = 0x00; //SCY
            this[ 0xFF43 ] = 0x00; //SCX
            this[ 0xFF45 ] = 0x00; //LYC
            this[ 0xFF47 ] = 0xFC; //BGP
            this[ 0xFF48 ] = 0xFF; //OBP0
            this[ 0xFF49 ] = 0xFF; //OBP1
            this[ 0xFF4A ] = 0x00; //WY
            this[ 0xFF4B ] = 0x00; //WX
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
                if (offset >= 0x0000 && offset < cartridgeMemoryEndOffset)
                {
                    return cartridge[offset];
                }
                if (offset >= cartridgeMemoryEndOffset && offset < videoRAMEndOffset)
                {
                    return gpu.ReadFromRAM( offset - cartridgeMemoryEndOffset);
                }
                if (offset >= videoRAMEndOffset && offset < switchableRAMBankEndOffset)
                {
                    return switchableRAMBankData[offset - videoRAMEndOffset];
                }
                if (offset >= switchableRAMBankEndOffset && offset < internalRAMEndOffset)
                {
                    return internalRAMData[offset - switchableRAMBankEndOffset];
                }
                if (offset >= internalRAMEndOffset && offset < echoInternalRAMEndOffset)
                {
                    return internalRAMData[offset - internalRAMEndOffset];
                }
                if (offset >= echoInternalRAMEndOffset && offset < spriteMemoryEndOffset)
                {
                    return spriteData[offset - echoInternalRAMEndOffset];
                }
                if (offset >= spriteMemoryEndOffset && offset < emptyExceptIOEndOffset)
                {
                    return emptyButIOData[offset - spriteMemoryEndOffset];
                }
                if (offset >= emptyExceptIOEndOffset && offset < ioPortsEndOffset)
                {
                    return ioPortsData[offset - emptyExceptIOEndOffset];
                }
                if (offset >= ioPortsEndOffset && offset < emptyExceptIOBisEndOffset)
                {
                    return emptyButIOBisData[offset - ioPortsEndOffset];
                }
                if (offset >= emptyExceptIOBisEndOffset && offset < internalRAMBisEndOffset)
                {
                    return internalRAMBisData[offset - emptyExceptIOBisEndOffset];
                }
                if (offset >= internalRAMBisEndOffset && offset < interruptEnableRegisterOffset)
                {
                    return internalRAMBisData[offset - internalRAMBisEndOffset];
                }

                return interruptEnableRegister;
            }
            set
            {
                if ( offset >= 0x0000 && offset < cartridgeMemoryEndOffset )
                {
                    cartridge[offset] = value;
                }
                else if (offset >= cartridgeMemoryEndOffset && offset < videoRAMEndOffset)
                {
                    gpu.WriteInRAM( offset - cartridgeMemoryEndOffset, value );
                }
                else if (offset >= videoRAMEndOffset && offset < switchableRAMBankEndOffset)
                {
                    switchableRAMBankData[ offset - videoRAMEndOffset ] = value;
                }
                else if (offset >= switchableRAMBankEndOffset && offset < internalRAMEndOffset)
                {
                    internalRAMData[ offset - switchableRAMBankEndOffset ] = value;
                }
                else if (offset >= internalRAMEndOffset && offset < echoInternalRAMEndOffset)
                {
                    internalRAMData[offset - internalRAMEndOffset] = value;
                }
                else if (offset >= echoInternalRAMEndOffset && offset < spriteMemoryEndOffset)
                {
                    spriteData[ offset - echoInternalRAMEndOffset ] = value;
                }
                else if (offset >= spriteMemoryEndOffset && offset < emptyExceptIOEndOffset)
                {
                    emptyButIOData[ offset - spriteMemoryEndOffset ] = value;
                }
                else if (offset >= emptyExceptIOEndOffset && offset < ioPortsEndOffset)
                {
                    ioPortsData[ offset - emptyExceptIOEndOffset ] = value;
                }
                else if (offset >= ioPortsEndOffset && offset < emptyExceptIOBisEndOffset)
                {
                    emptyButIOBisData[ offset - ioPortsEndOffset ] = value;
                }
                else if (offset >= emptyExceptIOBisEndOffset && offset < internalRAMBisEndOffset)
                {
                    internalRAMBisData[ offset - emptyExceptIOBisEndOffset ] = value;
                }
                else if (offset >= internalRAMBisEndOffset && offset < interruptEnableRegisterOffset)
                {
                    internalRAMBisData[ offset - internalRAMBisEndOffset ] = value;
                }
                else if (offset == interruptEnableRegisterOffset)
                {
                    interruptEnableRegister = value;
                }
            }
        }
    }
}
