using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GameboyEmulator
{
    public enum GPUMode
    {
        HBlankPeriod = 0x0,
        VBlankPeriod = 0x01,
        OAMReadMode = 0x02,
        VRAMReadMode = 0x03
    }
    
    public class GPU
    {
        private const int lcdLinesCount = 143;
        private const int hBlankCycleDuration = 204;
        private const int vBlankCycleDuration = 456;
        private const int oamRadModeCycleDuration = 80;
        private const int vRadModeCycleDuration = 172;

        private readonly Clock clock;
        private readonly Clock cpuClock;
        private readonly GPURegisters gpuRegisters;
        private GPUMode gpuMode;

        private readonly byte[] memoryData;
        private readonly byte[] tileSet;
        private readonly byte[] tileBackgroundMap;
        private readonly byte[] oamData;
        private readonly byte[] zRamData;

        private Bitmap bmp = new Bitmap(160, 144, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        public GPU( Clock cpuClock, GPURegisters gpuRegisters )
        {
            this.cpuClock = cpuClock;
            this.gpuRegisters = gpuRegisters;

            clock = new Clock();

            gpuMode = GPUMode.HBlankPeriod;

            memoryData = new byte[0x2000];
            tileSet = new byte[0x17FF];
            tileBackgroundMap = new byte[0x7FF];
            oamData = new byte[0xA0];
            zRamData = new byte[0x7F];

            bmp.Save("D:\\test.bmp");
        }

        public void FrameStep()
        {
            clock.IncrementCycleCount( cpuClock.LastCycleCountIncrement );

            switch (gpuMode)
            {
                case GPUMode.HBlankPeriod:
                    {
                        if (clock.CycleCount >= hBlankCycleDuration)
                        {
                            clock.Reset();
                            gpuRegisters.CurrentScanLine++;

                            if (gpuRegisters.CurrentScanLine == lcdLinesCount)
                            {
                                gpuMode = GPUMode.VBlankPeriod;
                                //TODO draw image

                                bmp.Save( "D:\\test.bmp" );
                            }
                            else
                            {
                                gpuMode = GPUMode.OAMReadMode;
                            }
                        }
                    }
                    break;
                case GPUMode.VBlankPeriod:
                    {
                        if (clock.CycleCount >= vBlankCycleDuration)
                        {
                            clock.Reset();
                            gpuRegisters.CurrentScanLine++;

                            if (gpuRegisters.CurrentScanLine > 153)
                            {
                                gpuMode = GPUMode.OAMReadMode;
                                gpuRegisters.CurrentScanLine = 0;
                            }
                        }
                    }
                    break;
                case GPUMode.OAMReadMode:
                    {
                        if (clock.CycleCount >= oamRadModeCycleDuration)
                        {
                            gpuMode = GPUMode.VRAMReadMode;
                            clock.Reset();
                        }
                    }
                    break;
                case GPUMode.VRAMReadMode:
                    {
                        if (clock.CycleCount >= vRadModeCycleDuration)
                        {
                            gpuMode = GPUMode.HBlankPeriod;
                            clock.Reset();

                            RenderScan();
                        }
                    }
                    break;
            }
        }

        public byte ReadFromRAM( int offset )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset < 0x17FF)
            {
                return tileSet[ offset ];
            }
            if (offset >= 0x1800 && offset < 0x1FFF)
            {
                return tileBackgroundMap[offset - 0x1800];
            }

            return memoryData[ offset ];
        }

        public void WriteInRAM( int offset, byte value )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset < 0x17FF )
            {
                tileSet[ offset ] = value;
            }
            else if (offset >= 0x1800 && offset < 0x1FFF)
            {
                tileBackgroundMap[offset - 0x1800] = value;
            }

            memoryData[ offset ] = value;
        }

        public byte ReadFromOAM( int offset )
        {
            return oamData[ offset ];
        }

        public void WriteToOAM(int offset, byte value)
        {
            oamData[ offset ] = value;
        }

        public byte ReadFromZeroPageRAM( int offset )
        {
            return zRamData[ offset ];
        }

        public void WriteToZeroPageRAM( int offset, byte value )
        {
            zRamData[ offset ] = value;
        }

        private unsafe void RenderScan()
        {
            var tileMapOffset = gpuRegisters.BackgroundTileMap == 1 ? 0x1C00 : 0x1800;
            tileMapOffset += (gpuRegisters.CurrentScanLine + gpuRegisters.ScrollY) >> 3;

            var lineOffset = gpuRegisters.ScrollX >> 3;

            var tile_line_offset = (gpuRegisters.CurrentScanLine + gpuRegisters.ScrollY) & 7;
            var tile_column_offset = gpuRegisters.ScrollX & 7;

            var tile = (int)tileSet[ tileMapOffset + lineOffset - 0x1800 ];
            
            if (gpuRegisters.BackgroundTileSet == 1 && tile < 128)
            {
                tile += 256;
            }

            //var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            //var bitmapLineOffset = (byte*)bmpData.Scan0 + (gpuRegisters.CurrentScanLine * 160 * 4);

            //for (var i = 0; i < 160; i++)
            //{
            //    *bitmapLineOffset = 0x0; // B
            //    *( bitmapLineOffset + 1 ) = 0xFF; // G
            //    *(bitmapLineOffset + 2) = 0x0; // R
            //    *(bitmapLineOffset + 3) = 0xFF; //A

            //    bitmapLineOffset += 4;

            //    x++;

            //    if (x == 8)
            //    {
            //        x = 0;
            //        lineOffset = (lineOffset + 1) & 31;

            //        tile = (int)tileSet[tileMapOffset + lineOffset - 0x1800];

            //        if (gpuRegisters.BackgroundTileSet == 1 && tile < 128)
            //        {
            //            tile += 256;
            //        }
            //    }
            //}

            //bmp.UnlockBits(bmpData);
        }
    }
}
