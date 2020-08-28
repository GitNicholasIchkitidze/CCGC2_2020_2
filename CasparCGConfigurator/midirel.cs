using System;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System.Threading;
using System.Windows.Forms;

namespace CasparCGConfigurator
{
    public class MidiRel
    {

        public static OutputDevice OutDevice = null;
        public InputDevice inDevice = null;
        public MidiRel()
        {
        }

        public static void InitMidiIn()
        {
            if (InputDevice.DeviceCount == 0)
            {

                
                MessageBox.Show("No MIDI input devices available.", "შეცდომა!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
               // Close();
            }
        }

        public void logMe(string strText)
        {
            MainForm.rtbLogs.Text += strText;
            rtbLogs.ScrollToCaret();
        }

    }
}