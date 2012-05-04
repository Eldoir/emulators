using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class GPURegisters
    {
        private byte[][] backgroundPalette;
        private byte[][] objectPalette0;
        private byte[][] objectPalette1;
        
        public byte ScrollX { get; private set; }
        public byte ScrollY { get; private set; }
        public byte WindowX { get; private set; }
        public byte WindowY { get; private set; }
        public byte CurrentScanLine { get; set; }
        public byte ScanLineCompare { get; private set; }
        public bool UseBackgrounds { get; private set; }
        public bool UseLCD { get; private set; }
        public int BackgroundTileMap { get; private set; }
        public int BackgroundTileSet { get; private set; }

        public GPURegisters()
        {
            backgroundPalette = new byte[4][];
            objectPalette0 = new byte[4][];
            objectPalette1 = new byte[4][];
        }

        public void Write( ushort offset, byte value )
        {
            switch ( offset )
            {
                // LCD Control
                case 0xFF40:
                    UseBackgrounds = ( value & 0x01 ) == 0x01;
                    BackgroundTileMap = ( value & 0x08 ) == 0x08 ? 1 : 0;
                    BackgroundTileSet = ( value & 0x10 ) == 0x10 ? 1 : 0;
                    UseLCD = ( value & 0x80 ) == 0x80;
                    break;

                // Scroll Y
                case 0xFF42:
                    ScrollY = value;
                    break;

                // Scroll X
                case 0xFF43:
                    ScrollX = value;
                    break;

                // LYC
                case 0xFF45:
                    ScanLineCompare = value;
                    break;

                // Background palette
                case 0xFF47:
                    SetPalette( ref backgroundPalette, value );
                    break;

                // Object palette 0
                case 0xFF48:
                    SetPalette( ref objectPalette0, value );
                    break;

                // Object palette 1
                case 0xFF49:
                    SetPalette( ref objectPalette1, value );
                    break;

                // Window X
                case 0xFF4A:
                    WindowX = value;
                    break;

                // Window Y
                case 0xFF4B:
                    WindowX = value;
                    break;
            }
        }

        public byte Read( ushort offset )
        {
            switch (offset)
            {
                // LCD Control
                case 0xFF40:
                    return ( byte ) ( ( UseBackgrounds ? 0x01 : 0x00 ) |
                                      ( BackgroundTileMap == 1 ? 0x08 : 0x00 ) |
                                      ( BackgroundTileSet == 1 ? 0x10 : 0x00 ) |
                                      ( UseLCD ? 0x80 : 0x00 ) );

                // Scroll Y
                case 0xFF42:
                    return ScrollY;
                
                // Scroll X
                case 0xFF43:
                    return ScrollX;
                
                // LY
                case 0xFF44:
                    return CurrentScanLine;

                // LYC
                case 0xFF45:
                    return ScanLineCompare;

                // WINDOW X
                case 0xFF4A:
                    return WindowX;

                // LYC
                case 0xFF4B:
                    return WindowY;
            }

            return 0;
        }

        private void SetPalette( ref byte[][] palette, byte value )
        {
            for ( var i = 0; i < 4; i++ )
            {
                switch ( ( value >> ( i * 2 ) ) & 3 )
                {
                    case 0:
                        palette[ i ] = new byte[] {255, 255, 255, 255};
                        break;
                    case 1:
                        palette[ i ] = new byte[] {192, 192, 192, 255};
                        break;
                    case 2:
                        palette[ i ] = new byte[] {96, 96, 96, 255};
                        break;
                    case 3:
                        palette[ i ] = new byte[] {0, 0, 0, 255};
                        break;
                }
            }
        }

        public byte[] GetPalette( byte offset )
        {
            return backgroundPalette[offset];
        }
    }
}
