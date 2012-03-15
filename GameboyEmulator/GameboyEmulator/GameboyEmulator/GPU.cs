using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        private GPUMode gpuMode;
        private int lineIndex;

        private readonly byte[] memoryData;
        private readonly List<Tile> tileSet;
        private readonly byte[] tileBackgroundMap1;
        private readonly byte[] tileBackgroundMap2;
        private readonly byte[] oamData;
        private readonly byte[] zRamData;

        public GPU( Clock cpuClock )
        {
            this.cpuClock = cpuClock;

            clock = new Clock();

            gpuMode = GPUMode.HBlankPeriod;
            lineIndex = 0;

            memoryData = new byte[0x2000];
            
            tileSet = new List< Tile >(192);

            for ( int i = 0; i < 384; i++ )
            {
                tileSet.Add( new Tile() );
            }

            tileBackgroundMap1 = new byte[32 * 32];
            tileBackgroundMap2 = new byte[32 * 32];
            oamData = new byte[0xA0];
            zRamData = new byte[0x7F];
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
                            lineIndex++;

                            if (lineIndex == lcdLinesCount)
                            {
                                gpuMode = GPUMode.VBlankPeriod;
                                //TODO draw image
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
                            lineIndex++;

                            if (lineIndex > 153)
                            {
                                gpuMode = GPUMode.OAMReadMode;
                                lineIndex = 0;
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

                            //TODO renderscan
                        }
                    }
                    break;
            }
        }

        public byte ReadFromRAM( int offset )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset <= 0x17FF)
            {
                var tileIndex = offset / 16;

                return tileSet[tileIndex][offset - tileIndex * 16];
            }
            if (offset >= 0x1800 && offset < 0x1BFF)
            {
                return tileBackgroundMap1[offset - 0x1800];
            }
            if (offset >= 0x1C00 && offset < 0x1FFF)
            {
                return tileBackgroundMap2[offset - 0x1C00];
            }

            return memoryData[ offset ];
        }

        public void WriteInRAM( int offset, byte value )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset <= 0x17FF )
            {
                var tileIndex = offset / 16;

                tileSet[ tileIndex ][ offset - tileIndex * 16 ] = value;
            }
            else if (offset >= 0x1800 && offset < 0x1BFF)
            {
                tileBackgroundMap1[offset - 0x1800] = value;
            }
            else if (offset >= 0x1C00 && offset < 0x1FFF)
            {
                tileBackgroundMap2[offset - 0x1C00] = value;
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
    }
}
