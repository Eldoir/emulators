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

namespace GameboyEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Emulator emulator = new Emulator();
        DispatcherTimer dispatcher = new DispatcherTimer();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // On initialise un dialog d'ouverture de fichiers
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            //ofd.DefaultExt = ".ch8";
            //ofd.Filter = "Chip8 ROM files (.ch8)|*.ch8";
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

                        Title = emulator.GameName;

                        dispatcher.Stop();

                        dispatcher.Interval = TimeSpan.FromSeconds(1 / 2);
                        dispatcher.Tick += new EventHandler(CPUCycle);
                        dispatcher.Start();

                    }
                }
            }
        }

        private void CPUCycle( object sender, EventArgs e )
        {
            emulator.EmulateFrame();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            emulator.KeyUp(e.Key);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            emulator.KeyDown(e.Key);
        }
    }
}
