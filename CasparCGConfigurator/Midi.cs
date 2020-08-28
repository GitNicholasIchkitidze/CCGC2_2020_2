using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Sanford.Multimedia.Midi;

namespace CasparCGConfigurator
{
    class Midi
    {
        public static SynchronizationContext contextMidi;
        public static OutputDevice OutDeviceMidi = null;
        public static void outDevice_Error_Midi(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(e.Error.Message, "Error!",
                   System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
        }

        public static void logMeMidi(string strText, MainForm form = null)
        {
            form.rtbLogs.Text += "\n" + strText;
            form.rtbLogs.ScrollToCaret();
        }
        public static UInt16 initMidi_Out_Midi(MainForm form = null)
        {
            if (OutputDevice.DeviceCount == 0)
            {
                System.Windows.Forms.MessageBox.Show("No MIDI Out devices available.", "შეცდომა!",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                //Close();
                form.cboBMidiOutDevs.Items.Clear();
                return 0;
            }
            else
            {
                try
                {


                    for (UInt16 i = 0; i < OutputDevice.DeviceCount; i++)
                    {

                        var modelOut = new OutputDevice(i);
                        var modelOutStr = modelOut.DeviceID.ToString();

                        logMeMidi("\nId " + i.ToString() + "; " + modelOutStr + " midi OUT device. Initializing MIDI interface... ",form);
                        form.cboBMidiOutDevs.Items.Add(i);
                        modelOut.Dispose();
                    }


                    logMeMidi("\nFound " + OutputDevice.DeviceCount.ToString() +
                        " midi OUT devices Total. Initializing MIDI interface... ",form);

                    return Convert.ToUInt16(OutputDevice.DeviceCount.ToString());
                    //context = SynchronizationContext.Current;
/*
                    OutDeviceMidi = new OutputDevice(1);
                    //OutDevice.ChannelMessageReceived += HandleChannelMessageReceived;
                    OutDeviceMidi.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(outDevice_Error_Midi);
                    //OutDevice..StartRecording();
                    logMeMidi("Done!\n",form);

*/
                }
                catch (Exception ex)
                {
                    logMeMidi("Error initializing MIDI interface: " + ex.Message,form);
                    //Close();
                    return 0;
                }
            }
        }

        public static bool setMidi_Out_Midi(MainForm form = null)
        {
            bool boolVar = true;
            
            try
            { 
                if ((OutDeviceMidi != null) && (!OutDeviceMidi.IsDisposed))
                {
                    OutDeviceMidi.Close();
                    OutDeviceMidi.Dispose();
                }
            OutDeviceMidi = new OutputDevice(Convert.ToUInt16(form.cboBMidiOutDevs.SelectedItem.ToString()));
            //OutDevice.ChannelMessageReceived += HandleChannelMessageReceived;
            OutDeviceMidi.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(outDevice_Error_Midi);
            //OutDevice..StartRecording();
            logMeMidi("Done!\n", form);
                boolVar = true;
            }
            catch (Exception ex)
            {
                logMeMidi("Error initializing MIDI interface: " + ex.Message, form);
                boolVar = false;
            }

            return boolVar;
        }

        public static void outDevice_Send_Midi(UInt16 MidiChannel, UInt16 Data1, UInt16 Data2, MainForm form = null)
        {

            

            ChannelMessageBuilder builder = new ChannelMessageBuilder();

            
            builder.Command = ChannelCommand.NoteOn;
            builder.MidiChannel = MidiChannel;
            builder.Data1 = Data1;
            builder.Data2 = Data2;
            builder.Build();

            OutDeviceMidi.Send(builder.Result);



        }


        public static void inDevice_Error_Midi(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(e.Error.Message, "Error!",
                   System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
        }

        public static void HandleChannelMessageReceived_Midi(object sender, ChannelMessageEventArgs e)
        {
            contextMidi.Post(delegate (object dummy)
            {
                
                    logMeMidi("Got MIDI " + e.Message.Command.ToString() +
                        " on channel " + (e.Message.MidiChannel + 1) + ", note: " +
                        e.Message.Data1 + "\n");
                
                if ((e.Message.Command.ToString() == "NoteOn") &&
                    (e.Message.MidiChannel + 1 == 10))
                {
                    if (e.Message.Data1 >= 48)
                    {
                        // 48 == C3
                        int iPlaylistItem = e.Message.Data1 - 48;
                        logMeMidi("Playing playlist item #" + iPlaylistItem.ToString() + "\n");
                        //PlayPlayListItem(iPlaylistItem + 4);
                    }
                }
            }, null);
        }


        public static void InitMidi_In_Midi(MainForm form = null)
        {
            if (InputDevice.DeviceCount == 0)
            {
                System.Windows.Forms.MessageBox.Show("No MIDI input devices available.", "შეცდომა!",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                // Close();
            }
            else
            {
                try
                {
                    logMeMidi("\nFound " + InputDevice.DeviceCount.ToString() +
                        " midi IN devices. Initializing MIDI interface... ");

                    for (UInt16 i = 0; i < InputDevice.DeviceCount; i++)
                    {

                        var modelIn = new InputDevice(i);

                        logMeMidi("\nid " + i.ToString() + "; " + modelIn.DeviceID + " midi IN device. Initializing MIDI interface... ");

                        modelIn.Dispose();
                    }


                    contextMidi = SynchronizationContext.Current;
                    form.inDevice = new InputDevice(0);
                    form.inDevice.ChannelMessageReceived += HandleChannelMessageReceived_Midi;
                    form.inDevice.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(inDevice_Error_Midi);
                    form.inDevice.StartRecording();
                    logMeMidi("Done!\n");
                }
                catch (Exception ex)
                {
                    logMeMidi("Error initializing MIDI interface: " + ex.Message);
                    //Close();
                }
            }

        }




    }
}
