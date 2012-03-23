using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace GameboyEmulator
{
    public class Keyboard
    {
        private byte[] rows = new byte[] { 0x0F, 0x0F };
        private byte column = 0;

        public byte Read()
        {
            switch (column)
            {
                case 0x10: return rows[0];
                case 0x20: return rows[1];
                default: return 0;
            }
        }

        public void Write( byte value )
        {
            column = (byte)(value & 0x30);
        }

        public void KeyUp(Key key)
        {
            switch ( key )
            {
                case Key.Right:
                    {
                        rows[1] &= 0xE;
                    }
                    break;
                case Key.Left:
                    {
                        rows[1] &= 0xD;
                    }
                    break;
                case Key.Up:
                    {
                        rows[1] &= 0xB;
                    }
                    break;
                case Key.Down:
                    {
                        rows[1] &= 0x7;
                    }
                    break;
                case Key.A:
                    {
                        rows[0] &= 0xE;
                    }
                    break;
                case Key.Z:
                    {
                        rows[0] &= 0xD;
                    }
                    break;
                case Key.Space:
                    {
                        rows[0] &= 0xB;
                    }
                    break;
                case Key.Enter:
                    {
                        rows[0] &= 0x7;
                    }
                    break;
            }
        }
        
        public void KeyDown(Key key)
        {
            switch (key)
            {
                case Key.Right:
                    {
                        rows[1] |= 0x1;
                    }
                    break;
                case Key.Left:
                    {
                        rows[1] |= 0x2;
                    }
                    break;
                case Key.Up:
                    {
                        rows[1] |= 0x4;
                    }
                    break;
                case Key.Down:
                    {
                        rows[1] |= 0x8;
                    }
                    break;
                case Key.A:
                    {
                        rows[0] |= 0x1;
                    }
                    break;
                case Key.Z:
                    {
                        rows[0] |= 0x2;
                    }
                    break;
                case Key.Space:
                    {
                        rows[0] |= 0x4;
                    }
                    break;
                case Key.Enter:
                    {
                        rows[0] |= 0x8;
                    }
                    break;
            }
        }
    }
}
