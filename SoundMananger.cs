using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.Diagnostics;
using System.Numerics;
using NAudio.Wave;
using System.Reflection;
using System.IO;
using Capital_and_Cargo.Properties;

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
                SoundPlayer.PlayLooping();
            }

        }
        public void stopMusic()
        {
            player.Stop();
        }
        public void playSound(System.IO.UnmanagedMemoryStream soundData)
        {
            Task.Run(() => soundThread(soundData));
        }
        private void soundThread(System.IO.UnmanagedMemoryStream soundData)
        {
            WaveOutEvent buttonSound = new WaveOutEvent();
            WaveFileReader audioReader;
            using (Stream stream = soundData)
            {
                if (stream != null)
                {
                    using(audioReader = new WaveFileReader(stream))
                    {
                        try
                        {
                            buttonSound.Init(audioReader);


                            buttonSound.Play();
                            while (buttonSound.PlaybackState == PlaybackState.Playing)
                            {
                                Thread.Sleep(50);
                            }

                        }
                        catch (Exception ex ){
                            Debug.Write(ex);
                        }
                    }
                    
                }
            }
            



        }
    }
}
