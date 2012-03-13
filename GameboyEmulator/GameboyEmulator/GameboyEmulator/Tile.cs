using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    class Tile
    {
        private readonly byte[] data = new byte[16];

        public byte this[ int offset ]
        {
            get { return data[ offset ]; }
            set { data[ offset ] = value; }
        }
    }
}
