using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.Diagnostics;
using System.Numerics;

namespace Capital_and_Cargo
{
    internal class SoundMananger
    {
        //private static string audioFilePath = "\\Capital-and-Cargo\\sound\\music\\gameMusic.wav";

        // Create a new SoundPlayer instance with the path to the .wav audio file
        private SoundPlayer player;
        private Program program;
        public SoundMananger() 
        {
            this.player = new SoundPlayer(Properties.Resources.gameMusic);
            this.program = new Program();
        }

        public void playMusic()
        {

            using (SoundPlayer SoundPlayer = new SoundPlayer(Properties.Resources.gameMusic))
            {
                SoundPlayer.Play();
            }

        }
        public void stopMusic()
        {
            player.Stop();
        }
        public void playSound(System.IO.UnmanagedMemoryStream soundData)
        {
            using (SoundPlayer SoundPlayer = new SoundPlayer(soundData))
            {
                SoundPlayer.Play();
            }

            

        }
    }
}
