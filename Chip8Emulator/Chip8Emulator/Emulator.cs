using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chip8Emulator
{
    public class Emulator
    {
        // Mémoire principale
        // Chip8 possède "4 ko de mémoire 8bit", soit 4096 blocs de 8bit
        // On transpose ca en un tableau de 4096 "byte", le type byte étant un entier
        // non signé codé sur 8 bits
        private byte[] memory = new byte[4096];

        // Program Counter
        // Chip8 possède "un compteur PC, 12bit"
        // Un type de 12bit n'existe pas, on va donc utiliser un type de borne supérieure,
        // en l'occurence ushort (entier non signé sur 16 bits)
        // La documentation dit qu'un programme démarre à l'adresse 0x200h
        private ushort programCounter = 0x200;

        // Registres
        // Chip8 possède "16 registres 8bit nommés V0, V1, V2… VF"
        // Pour travailler avec ces 16 registres plus facilement, nous allons en faire
        // un tableau de 16 valeurs (indexé de 0x0 à 0xF)
        private byte[] V = new byte[16];
        
        // Plus "1 registre 16bit nommé I"
        private ushort I;

        private byte[] screenBuffer = new byte[8192];

        private Stack<ushort> callStack = new Stack<ushort>();

        private Random random = new Random();

        public bool[] Keyboard = new bool[16];

        private byte delayTimer = 0;
        public byte soundTimer = 0;

        // Les paramètres possibles d'une instruction
        private ushort NNN;
        private byte KK;
        private byte K;
        private byte X;
        private byte Y;

        public Emulator()
        {
            memory[0] = 240; memory[1] = 144; memory[2] = 144; memory[3] = 144; memory[4] = 240;
            memory[5] = 32; memory[6] = 96; memory[7] = 32; memory[8] = 32; memory[9] = 112;
            memory[10] = 240; memory[11] = 16; memory[12] = 240; memory[13] = 128; memory[14] = 240;
            memory[15] = 240; memory[16] = 16; memory[17] = 240; memory[18] = 16; memory[19] = 240;
            memory[20] = 144; memory[21] = 144; memory[22] = 240; memory[23] = 16; memory[24] = 16;
            memory[25] = 240; memory[26] = 128; memory[27] = 240; memory[28] = 16; memory[29] = 240;
            memory[30] = 240; memory[31] = 128; memory[32] = 240; memory[33] = 144; memory[34] = 240;
            memory[35] = 240; memory[36] = 16; memory[37] = 32; memory[38] = 64; memory[39] = 64;
            memory[40] = 240; memory[41] = 144; memory[42] = 240; memory[43] = 144; memory[44] = 240;
            memory[45] = 240; memory[46] = 144; memory[47] = 240; memory[48] = 16; memory[49] = 240;
            memory[50] = 240; memory[51] = 144; memory[52] = 240; memory[53] = 144; memory[54] = 144;
            memory[55] = 224; memory[56] = 144; memory[57] = 224; memory[58] = 144; memory[59] = 224;
            memory[60] = 240; memory[61] = 128; memory[62] = 128; memory[63] = 128; memory[64] = 240;
            memory[65] = 224; memory[66] = 144; memory[67] = 144; memory[68] = 144; memory[69] = 224;
            memory[70] = 240; memory[71] = 128; memory[72] = 240; memory[73] = 128; memory[74] = 240;
            memory[75] = 240; memory[76] = 128; memory[77] = 240; memory[78] = 128; memory[79] = 128;
        }

        // Charge une ROM en mémoire
        public void Load(byte[] rom)
        {
            // On fait une bête copie en mémoire de la ROM
            // Mais attention, la doc dit que "les programmes Chip8
            // commencent à l'adresse 0x200h, car les 512 premiers blocs
            // sont réservés au CPU"
            for (int i = 0; i < rom.Length; i++)
            {
                memory[0x200 + i] = rom[i];
            }

            Reset();
        }

        public void Reset()
        {
            // On vide la pile d'appel
            callStack.Clear();
            // On reset PC
            programCounter = 0x200;
            // Et l'écran
            Inst_00E0();
        }

        public byte[] GetScreenBuffer()
        {
            lock (screenBuffer)
            {
                return screenBuffer;
            }
        }

        // Emule un cycle du CPU
        public void Emulate()
        {
            // On récupère l'instruction 16bit sur laquelle pointe PC
            // Pour ca on lit 2 byte 8bit que l'on stock sur 16 bits
            ushort opcode = (ushort)((memory[programCounter] << 8) + memory[programCounter + 1]);

            // Puis on incrémente PC d'autant de blocs que l'on vient de lire, soit 2
            programCounter += 2;

            // On extrait les différents paramètres possibles d'une instruction à l'avance
            ushort _Code = (ushort)(opcode & 0xF000);
            NNN = (ushort)(opcode & 0x0FFF);
            KK = (byte)(opcode & 0x00FF);
            K = (byte)(opcode & 0x000F);
            X = (byte)((opcode & 0x0F00) >> 8);
            Y = (byte)((opcode & 0x00F0) >> 4);

            // Maintenant on décode l'opcode et on le redirige vers la bonne instruction (dispatcher)
            switch (_Code)
            {
                case 0x0000:
                    switch (KK)
                    {
                        case 0xE0: Inst_00E0(); break;
                        case 0xEE: Inst_00EE(); break;
                    }
                    break;
                case 0x1000: Inst_1NNN(); break;
                case 0x2000: Inst_2NNN(); break;
                case 0x3000: Inst_3XKK(); break;
                case 0x4000: Inst_4XKK(); break;
                case 0x5000: Inst_5XY0(); break;
                case 0x6000: Inst_6XKK(); break;
                case 0x7000: Inst_7XKK(); break;
                case 0x8000:
                    switch (K)
                    {
                        case 0x0: Inst_8XY0(); break;
                        case 0x1: Inst_8XY1(); break;
                        case 0x2: Inst_8XY2(); break;
                        case 0x3: Inst_8XY3(); break;
                        case 0x4: Inst_8XY4(); break;
                        case 0x5: Inst_8XY5(); break;
                        case 0x6: Inst_8XY6(); break;
                        case 0x7: Inst_8XY7(); break;
                        case 0xE: Inst_8XYE(); break;
                    }
                    break;
                case 0x9000: Inst_9XY0(); break;
                case 0xA000: Inst_ANNN(); break;
                case 0xB000: Inst_BNNN(); break;
                case 0xC000: Inst_CXKK(); break;
                case 0xD000: Inst_DXYK(); break;
                case 0xE000:
                    switch (KK)
                    {
                        case 0x9E: Inst_EX9E(); break;
                        case 0xA1: Inst_EXA1(); break;
                    }
                    break;
                case 0xF000:
                    switch (KK)
                    {
                        case 0x07: Inst_FX07(); break;
                        case 0x0A: Inst_FX0A(); break;
                        case 0x15: Inst_FX15(); break;
                        case 0x18: Inst_FX18(); break;
                        case 0x1E: Inst_FX1E(); break;
                        case 0x29: Inst_FX29(); break;
                        case 0x33: Inst_FX33(); break;
                        case 0x55: Inst_FX55(); break;
                        case 0x65: Inst_FX65(); break;
                    }
                    break;
            }

            if (delayTimer > 0)
            {
                delayTimer--;
            }

            if (soundTimer > 0)
            {
                soundTimer--;
            }
        }

        private void Inst_FX65()
        {
            for (int i = 0; i <= X; i++)
            {
                V[i] = memory[I + i];
            }
        }

        private void Inst_FX55()
        {
            for (int i = 0; i <= X; i++)
            {
                memory[I + i] = V[i];
            }
        }

        private void Inst_FX33()
        {
            memory[I]       = (byte)(V[X] / 100);
            memory[I+1]     = (byte)(V[X] % 100 / 10);
            memory[I + 2]   = (byte)(V[X] % 100 % 10);
        }

        private void Inst_FX29()
        {
            I = (ushort)(V[X] * 5);
        }

        private void Inst_FX1E()
        {
            I += V[X];
        }

        private void Inst_FX18()
        {
            soundTimer = V[X];
        }

        private void Inst_FX15()
        {
            delayTimer = V[X];
        }

        private void Inst_FX0A()
        {
            while (true)
            {
                for (byte i = 0; i < 16; i++)
                {
                    if (Keyboard[i] == true)
                    {
                        V[X] = i;
                        return;
                    }
                }
            }
        }

        private void Inst_FX07()
        {
            V[X] = delayTimer;
        }

        private void Inst_EXA1()
        {
            if (!Keyboard[V[X]])
            {
                programCounter += 2;
            }
        }

        private void Inst_EX9E()
        {
            if ( Keyboard[V[X]])
            {
                programCounter += 2;
            }
        }

        private void Inst_DXYK()
        {
            // On reset le flag
            V[0xF] = 0;

            // Pour chaque ligne de pixels à écrire
            for (int i = 0; i < K; i++)
            {
                byte ligne = memory[I + i];

                // Pour chaque pixel dans la ligne
                for (int n = 0; n < 8; n++)
                {
                    // On extrait le pixel à écrire
                    byte pixel = (byte)((byte)(ligne << n) >> 7);

                    // On calcul l'adresse du pixel dans le frame buffer
                    int pixelAddress = V[X] + n + (V[Y] + i) * 64;
                    pixelAddress *= 4;
                    pixelAddress %= screenBuffer.Length;

                    // On fait la somme des composantes, pour savoir si il y a une couleur
                    int couleur = screenBuffer[pixelAddress] + screenBuffer[pixelAddress + 1] + screenBuffer[pixelAddress + 2];
                    
                    // On l'écrit en XOR
                    lock (screenBuffer)
                    {
                        if ((pixel != 0 && couleur == 0) || (pixel == 0 && couleur != 0))
                        {
                            screenBuffer[pixelAddress] = 255;
                            screenBuffer[pixelAddress + 1] = 255;
                            screenBuffer[pixelAddress + 2] = 255;
                            screenBuffer[pixelAddress + 3] = 255;
                        }
                        else
                        {
                            screenBuffer[pixelAddress] = 0;
                            screenBuffer[pixelAddress + 1] = 0;
                            screenBuffer[pixelAddress + 2] = 0;
                            screenBuffer[pixelAddress + 3] = 255;
                        }
                    }

                    // Et on détecte la collision
                    if (pixel != 0 && couleur != 0)
                    {
                        V[0xF] = 1;
                    }
                }
            }
        }

        private void Inst_CXKK()
        {
            V[X] = (byte)(KK & (byte)random.Next(0, 256));
        }

        private void Inst_BNNN()
        {
            programCounter = (ushort)(NNN + V[0]);
        }

        private void Inst_ANNN()
        {
            I = NNN;
        }

        private void Inst_9XY0()
        {
            if (V[X] != V[Y])
            {
                programCounter += 2;
            }
        }

        private void Inst_8XYE()
        {
            V[0xF] = (byte)((V[X] & 0x80) >> 7);
            V[X] <<= 1;
        }

        private void Inst_8XY7()
        {
            if (V[Y] > V[X])
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }

            V[X] = (byte)(V[Y] - V[X]);
        }

        private void Inst_8XY6()
        {
            V[0xF] = (byte)(V[X] & 0x1);
            V[X] >>= 1;
        }

        private void Inst_8XY5()
        {
            if (V[X] > V[Y])
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }

            V[X] -= V[Y];
        }

        private void Inst_8XY4()
        {
            int tmp = V[X] + V[Y];

            if (tmp > 255)
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }

            V[X] = (byte)(tmp & 0xFF);
        }

        private void Inst_8XY3()
        {
            V[X] ^= V[Y];
        }

        private void Inst_8XY2()
        {
            V[X] &= V[Y];
        }

        private void Inst_8XY1()
        {
            V[X] |= V[Y];
        }

        private void Inst_8XY0()
        {
            V[X] = V[Y];
        }

        private void Inst_7XKK()
        {
            V[X] += KK;
        }

        private void Inst_6XKK()
        {
            V[X] = KK;
        }

        private void Inst_5XY0()
        {
            if (V[X] == V[Y])
            {
                programCounter += 2;
            }
        }

        private void Inst_4XKK()
        {
            if (V[X] != KK)
            {
                programCounter += 2;
            }
        }

        private void Inst_3XKK()
        {
            if (V[X] == KK)
            {
                programCounter += 2;
            }
        }

        private void Inst_2NNN()
        {
            callStack.Push(programCounter);
            programCounter = NNN;
        }

        private void Inst_1NNN()
        {
            programCounter = NNN;
        }

        private void Inst_00EE()
        {
            programCounter = callStack.Pop();
        }

        private void Inst_00E0()
        {
            // noir = {B = 0, G = 0, R = 0, A = 255}
            for (int i = 0; i < screenBuffer.Length; i += 4)
            {
                screenBuffer[i] = 0;
                screenBuffer[i + 1] = 0;
                screenBuffer[i + 2] = 0;
                screenBuffer[i + 3] = 255;
            }
        }
    }
}
