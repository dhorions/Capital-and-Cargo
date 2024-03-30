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
        public SoundMananger() 
        {
            this.player = new SoundPlayer(Properties.Resources.gameMusic);
        }

        public void playMusic()
        {

            try
            {
                // Play the .wav audio file
                //player.Play();
                player.PlayLooping();
                Debug.WriteLine("music plays");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        //    finally
        //    {
        //        // Dispose the SoundPlayer object to release resources
        //        player.Dispose();
        //    }
        }
        private void stopMusic()
        {
            player.Stop();
        }
    }
}
