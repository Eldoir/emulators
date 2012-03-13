using System;

namespace GameboyEmulator
{
    public class Cartridge
    {
        private readonly byte[] romData;

        private static readonly byte[] nintendoLogo = new byte[]
                                                          {
                                                              0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
                                                              0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
                                                              0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
                                                          };
        private const int nintendoLogoOffset = 0x104;
        private const int gameNameOffset = 0x134;
        private const int gameNameLength = 0xF;
        private const int isGameBoyColorOffset = 0x143;
        private const int isSuperGameBoyOffset = 0x146;
        private const int cartridgeTypeOffset = 0x147;
        private const int romSizeOffset = 0x148;
        private const int ramSizeOffset = 0x149;
        private const int checksumStartOffset = 0x134;
        private const int checksumEndOffset = 0x14D;

        private const int romBankSize = 0x10;

        public Cartridge( byte[] romData )
        {
            this.romData = new byte[romData.Length];

            Array.Copy( romData, this.romData, romData.Length );
        }

        public byte this[ ushort offset ]
        {
            get { return romData[ offset ]; }
            set { romData[ offset ] = value; }
        }

        public string GameName { get; private set; }
        public GameBoyType GameBoyType { get; private set; }
        public int ROMSize { get; private set; }
        public int ROMBankCount { get; private set; }
        public int RAMSize { get; private set; }
        public int RAMBankCount { get; private set; }

        public void Initialize()
        {
            CheckNintendoLogo();
            SetGameName();
            SetHardware();
            CheckChecksum();
        }

        private void CheckChecksum()
        {
            ushort sum = 0;

            for ( int i = checksumStartOffset; i < checksumEndOffset; i++ )
            {
                sum += romData[ i ];
            }

            sum += 25;

            if ((sum & 0xFF) != 0)
            {
                Console.WriteLine( "Checksum check failed" );
            }
        }

        private void SetHardware()
        {
            SetGameBoyType();
            SetCartridgeType();
            SetROMSize();
            SetRAMSize();
            

            Console.WriteLine( romData[ cartridgeTypeOffset ] );
        }

        private void SetRAMSize()
        {
            switch ( romData[ ramSizeOffset ] )
            {
                case 0:
                    RAMBankCount = 0;
                    RAMSize = 0;
                    break;
                case 1:
                    RAMBankCount = 1;
                    RAMSize = 2;
                    break;
                case 2:
                    RAMBankCount = 1;
                    RAMSize = 8;
                    break;
                case 3:
                    RAMBankCount = 4;
                    RAMSize = 32;
                    break;
                case 4:
                    RAMBankCount = 16;
                    RAMSize = 128;
                    break;
            }
        }

        private void SetROMSize()
        {
            switch ( romData[romSizeOffset] )
            {
                case 0:
                    ROMSize = 32;
                    ROMBankCount = 2;
                    break;
                case 1:
                    ROMSize = 64;
                    ROMBankCount = 4;
                    break;
                case 2:
                    ROMSize = 128;
                    ROMBankCount = 8;
                    break;
                case 3:
                    ROMSize = 256;
                    ROMBankCount = 16;
                    break;
                case 4:
                    ROMSize = 512;
                    ROMBankCount = 32;
                    break;
                case 5:
                    ROMSize = 1024;
                    ROMBankCount = 64;
                    break;
                case 6:
                    ROMSize = 2048;
                    ROMBankCount = 128;
                    break;
                case 0x52:
                    ROMSize = 1152;
                    ROMBankCount = 72;
                    break;
                case 0x53:
                    ROMSize = 1280;
                    ROMBankCount = 80;
                    break;
                case 0x54:
                    ROMSize = 1536;
                    ROMBankCount = 96;
                    break;
            }

            ROMSize = ROMBankCount * romBankSize;
        }

        private void SetCartridgeType()
        {
            switch (romData[cartridgeTypeOffset])
            {
                case 0:

                    break;
            }
        }

        private void SetGameBoyType()
        {
            if ( romData[ isGameBoyColorOffset ] == 0x80 )
            {
                GameBoyType = GameBoyType.GameBoyColor;
            }
            else if ( romData[ isSuperGameBoyOffset ] == 0x03 )
            {
                GameBoyType = GameBoyType.SuperGameBoy;
            }
            else
            {
                GameBoyType = GameBoyType.GameBoy;
            }
        }

        private void SetGameName()
        {
            var gameNameBytes = new byte[gameNameLength];

            Array.Copy(romData, gameNameOffset, gameNameBytes, 0, gameNameLength);

            GameName = System.Text.Encoding.ASCII.GetString(gameNameBytes);
        }

        private void CheckNintendoLogo()
        {
            for (int x = nintendoLogoOffset, y = 0; y < nintendoLogo.Length; x++, y++)
            {
                if (nintendoLogo[y] != romData[x])
                {
                    throw new Exception( "The validation of the Nintendo logo has failed" );
                }
            }
        }
    }
}
