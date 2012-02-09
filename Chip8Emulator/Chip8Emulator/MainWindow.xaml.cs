using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace Chip8Emulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Emulator emulator = new Emulator();
        // Le timer / thread pour le rendu
        DispatcherTimer RenderTimer = new DispatcherTimer();
        // Le bitmap pour afficher le screen buffer
        WriteableBitmap writeableBitmap = new WriteableBitmap(64, 32, 60, 60, PixelFormats.Bgra32, null);
        // Le timer pour le timing CPU
        DispatcherTimer Chip8Timer = new DispatcherTimer();


        public MainWindow()
        {
            InitializeComponent();
            frameBufferImage.Source = writeableBitmap;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // On initialise un dialog d'ouverture de fichiers
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.DefaultExt = ".ch8";
            ofd.Filter = "Chip8 ROM files (.ch8)|*.ch8";
            ofd.Multiselect = false;

            // On ouvre le dialog
            Nullable<bool> result = ofd.ShowDialog();

            // Si un fichier a été sélectionné
            if (result == true)
            {
                // On récupère son chemin
                string filename = ofd.FileName;

                // Et on l'ouvre
                using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open))
                {
                    // On l'ouvre en mode binaire
                    using (System.IO.BinaryReader br = new System.IO.BinaryReader(fs))
                    {
                        // Et on le mets dans un tableau de byte
                        byte[] rom = new byte[fs.Length];

                        for (int i = 0; i < fs.Length; i++)
                        {
                            rom[i] = br.ReadByte();
                        }

                        // Puis on le donne à notre émulateur
                        emulator.Load(rom);

                        // On stop l'émulation le temps du chargement
                        Chip8Timer.Stop();
                        // Puis on le donne à notre émulateur
                        
                        RenderTimer.Interval = TimeSpan.FromSeconds(1 / 5);
                        RenderTimer.Tick += new EventHandler(Render);
                        RenderTimer.Start();

                        Chip8Timer.Interval = TimeSpan.FromSeconds(1 / 2);
                        Chip8Timer.Tick += new EventHandler(CPUCycle);
                        Chip8Timer.Start();
                    }
                }
            }
        }

        private void CPUCycle(object sender, EventArgs e)
        {
            emulator.Emulate();
        }

        private void Render(object sender, EventArgs e)
        {
            byte[] data = emulator.GetScreenBuffer();
            if (data != null)
            {
                writeableBitmap.Lock();
                Marshal.Copy(data, 0, writeableBitmap.BackBuffer, data.Length);
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 64, 32));
                writeableBitmap.Unlock();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.NumPad0)
                emulator.Keyboard[0x0] = true;
            if (e.Key == Key.NumPad7)
                emulator.Keyboard[0x1] = true;
            if (e.Key == Key.NumPad8)
                emulator.Keyboard[0x2] = true;
            if (e.Key == Key.NumPad9)
                emulator.Keyboard[0x3] = true;
            if (e.Key == Key.NumPad4)
                emulator.Keyboard[0x4] = true;
            if (e.Key == Key.NumPad5)
                emulator.Keyboard[0x5] = true;
            if (e.Key == Key.NumPad6)
                emulator.Keyboard[0x6] = true;
            if (e.Key == Key.NumPad1)
                emulator.Keyboard[0x7] = true;
            if (e.Key == Key.NumPad2)
                emulator.Keyboard[0x8] = true;
            if (e.Key == Key.NumPad3)
                emulator.Keyboard[0x9] = true;
            if (e.Key == Key.OemPlus)
                emulator.Keyboard[0xA] = true;
            if (e.Key == Key.OemPeriod)
                emulator.Keyboard[0xB] = true;
            if (e.Key == Key.Divide)
                emulator.Keyboard[0xC] = true;
            if (e.Key == Key.Multiply)
                emulator.Keyboard[0xD] = true;
            if (e.Key == Key.OemMinus)
                emulator.Keyboard[0xE] = true;
            if (e.Key == Key.Enter)
                emulator.Keyboard[0xF] = true;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.NumPad0)
                emulator.Keyboard[0x0] = false;
            if (e.Key == Key.NumPad7)
                emulator.Keyboard[0x1] = false;
            if (e.Key == Key.NumPad8)
                emulator.Keyboard[0x2] = false;
            if (e.Key == Key.NumPad9)
                emulator.Keyboard[0x3] = false;
            if (e.Key == Key.NumPad4)
                emulator.Keyboard[0x4] = false;
            if (e.Key == Key.NumPad5)
                emulator.Keyboard[0x5] = false;
            if (e.Key == Key.NumPad6)
                emulator.Keyboard[0x6] = false;
            if (e.Key == Key.NumPad1)
                emulator.Keyboard[0x7] = false;
            if (e.Key == Key.NumPad2)
                emulator.Keyboard[0x8] = false;
            if (e.Key == Key.NumPad3)
                emulator.Keyboard[0x9] = false;
            if (e.Key == Key.OemPlus)
                emulator.Keyboard[0xA] = false;
            if (e.Key == Key.OemPeriod)
                emulator.Keyboard[0xB] = false;
            if (e.Key == Key.Divide)
                emulator.Keyboard[0xC] = false;
            if (e.Key == Key.Multiply)
                emulator.Keyboard[0xD] = false;
            if (e.Key == Key.OemMinus)
                emulator.Keyboard[0xE] = false;
            if (e.Key == Key.Enter)
                emulator.Keyboard[0xF] = false;
        }
    }
}
