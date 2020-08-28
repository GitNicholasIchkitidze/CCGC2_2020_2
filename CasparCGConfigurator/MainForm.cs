using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;

using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using System.Threading;

namespace CasparCGConfigurator
{
    
    public partial class MainForm : Form
//    public partial class MainForm : Form
    {
        
        public configuration config = new configuration();
        private ConsumerControlBase consumerEditorControl;
        private AbstractConsumer lastConsumer;
        public List<String> availableDecklinkIDs = new List<string>();

        public OutputDevice OutDevice = null;
        public InputDevice inDevice = null;
        private SynchronizationContext context;

        string Q_4_CORRECT_Ans = "";
        string Q_3_CORRECT_Ans = "";
        string Q_2_CORRECT_Ans = "";

        System.Net.Sockets.TcpClient casparClient = new System.Net.Sockets.TcpClient();

        public MainForm()
        {
            this.InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            if (System.IO.File.Exists("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config"))
                DeSerializeConfig(System.IO.File.ReadAllText("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config"));
            else
            {
                System.Windows.Forms.MessageBox.Show("A 'casparcg.config' file was not found in the same directory as this application.  One is now being generated.","CasparCG Configurator",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                SerializeConfig();
            }
            this.WireBindings();
            this.Updatechannel();
            this.SetToolTips();

            //InitMidi_In();
            initMidi_Out();


            cBox_QuestionLayer.SelectedIndex = 0;
            cBox_AnswersLayer.SelectedIndex = 0;
        }

        private void WireBindings()
        {
            this.pathsBindingSource.DataSource = this.config.Paths;
            this.flashBindingSource.DataSource = this.config.Flash;
            this.configurationBindingSource.DataSource = this.config;
            this.listBox1.DataSource = this.config.Channels;           
        }

        private void SerializeConfig()
        {
            var extraTypes = new Type[1]{typeof(AbstractConsumer)};

            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using(var writer = doc.CreateWriter())
            {
                new XmlSerializer(typeof(configuration), extraTypes).Serialize(writer, config, namespaces);
            }

            doc.Element("configuration").Add(
                new XElement("controllers",
                    new XElement("tcp",
                        new XElement[2]
                        {
                            new XElement("port", 5250),
                            new XElement("protocol", "AMCP")
                        })));

            doc.Add(new XComment(CasparCGConfigurator.Properties.Resources.configdoc.ToString()));

            using (var writer = new XmlTextWriter("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config", new UTF8Encoding(false, false))) // No BOM
            {
                writer.Formatting = Formatting.Indented;
                doc.Save(writer);
            }
        }
        
        private void DeSerializeConfig(string text)
        {
            var x = new XmlSerializer(typeof(configuration));

            using (var reader = new StringReader(text))
            {
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);

                try
                {
                    this.config = (configuration)x.Deserialize(reader);
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("There was an error reading the current 'casparcg.config' file.  A new one will be generated.","CasparCG Configurator", MessageBoxButtons.OK,MessageBoxIcon.Error);
                    this.config = new configuration();
                }
            }
            this.WireBindings();
        }

        private void RefreshConsumerPanel()
        {
            if (lastConsumer != listBox2.SelectedItem)
            {

                this.panel1.Controls.Clear();
                if (consumerEditorControl != null)
                    consumerEditorControl.Dispose();

                this.consumerEditorControl = null;

                if (listBox2.SelectedItems.Count > 0)
                {
                    if (listBox2.SelectedItem.GetType() == typeof(DecklinkConsumer))
                    {
                        this.consumerEditorControl = new DecklinkConsumerControl(listBox2.SelectedItem as DecklinkConsumer,config.AvailableDecklinkIDs);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(ScreenConsumer))
                    {
                        this.consumerEditorControl = new ScreenConsumerControl(listBox2.SelectedItem as ScreenConsumer);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(BluefishConsumer))
                    {
                        this.consumerEditorControl = new BluefishConsumerControl(listBox2.SelectedItem as BluefishConsumer,config.AvailableBluefishIDs);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                }
            }
            lastConsumer = (AbstractConsumer)listBox2.SelectedItem;
        }

        private void Updatechannel()
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                this.comboBox1.Enabled = true;
                this.listBox2.Enabled = true;
                this.button4.Enabled = true;
                this.button5.Enabled = true;
                this.button7.Enabled = true;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.listBox2.DataSource = ((Channel)listBox1.SelectedItem).Consumers;
                this.comboBox1.SelectedItem = ((Channel)listBox1.SelectedItem).VideoMode;
            }
            else
            {
                this.comboBox1.Enabled = false;
                this.listBox2.Enabled = false;
                this.button4.Enabled = false;
                this.button5.Enabled = false;
                this.button7.Enabled = false;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.listBox2.DataSource = null;
                this.comboBox1.SelectedItem = null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.config.Channels.AddNew();
            this.Updatechannel();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Updatechannel();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new DecklinkConsumer(config.AvailableDecklinkIDs));

            RefreshConsumerPanel();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new ScreenConsumer());
            this.RefreshConsumerPanel();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)            
                (listBox1.SelectedItem as Channel).VideoMode = comboBox1.SelectedItem.ToString();            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)            
                this.config.Channels.Remove((Channel)listBox1.SelectedItem);
            
            this.Updatechannel();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)            
                (listBox1.SelectedItem as Channel).Consumers.Remove(listBox2.SelectedItem as AbstractConsumer);
            
            this.RefreshConsumerPanel();
        }

        private void showXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SerializeConfig();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.RefreshConsumerPanel();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var res = System.Windows.Forms.MessageBox.Show("Do you want to save this configuration before exiting?", "CasparCG Configurator", MessageBoxButtons.YesNoCancel);
            if (res == System.Windows.Forms.DialogResult.Yes || res == System.Windows.Forms.DialogResult.OK)
                SerializeConfig();
            //else if(res == System.Windows.Forms.DialogResult.No)           
            else if(res == System.Windows.Forms.DialogResult.Cancel)            
                e.Cancel = true; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new SystemAudioConsumer());
            RefreshConsumerPanel();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new BluefishConsumer(config.AvailableBluefishIDs));
            RefreshConsumerPanel();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            setTextboxFilepath(datapathTextBox);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            setTextboxFilepath(logpathTextBox);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            setTextboxFilepath(mediapathTextBox);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            setTextboxFilepath(templatepathTextBox);
        }

        private void setTextboxFilepath(TextBox control)
        {
            using (var fd = new FolderBrowserDialog())
            {
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var p = fd.SelectedPath;
                    control.Text = fd.SelectedPath + (p.EndsWith("\\") ? "" : "\\");
                }
            }
        }

        private void SetToolTips()
        {
            var toolTip = new ToolTip();
            //toolTip.SetToolTip(this.##CONTROL##, "##Tooltip text##");
            toolTip.SetToolTip(this.pipelineTokensComboBox, "This will set the mixer buffer depth.");
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void rtbLogs_TextChanged(object sender, EventArgs e)
        {

        }


        public void InitMidi_In()
        {
            if (InputDevice.DeviceCount == 0)
            {
                MessageBox.Show("No MIDI input devices available.", "შეცდომა!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                // Close();
            }
            else
            {
                try
                {
                    logMe("\nFound " + InputDevice.DeviceCount.ToString() +
                        " midi IN devices. Initializing MIDI interface... ");

                    for (UInt16 i = 0; i < InputDevice.DeviceCount; i++)
                    {

                        var modelIn = new InputDevice(i);

                        logMe("\nid " + i.ToString() + "; " + modelIn.DeviceID + " midi IN device. Initializing MIDI interface... ");

                        modelIn.Dispose();
                    }


                    context = SynchronizationContext.Current;
                    inDevice = new InputDevice(0);
                    inDevice.ChannelMessageReceived += HandleChannelMessageReceived;
                    inDevice.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(inDevice_Error);
                    inDevice.StartRecording();
                    logMe("Done!\n");
                }
                catch (Exception ex)
                {
                    logMe("Error initializing MIDI interface: " + ex.Message);
                    //Close();
                }
            }

        }

        public void logMe(string strText)
        {
            rtbLogs.Text += "\n" + strText;
            rtbLogs.ScrollToCaret();
        }


        private void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            MessageBox.Show(e.Error.Message, "Error!",
                   MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            context.Post(delegate (object dummy)
            {
                if (cbDebug.Checked)
                {
                    logMe("Got MIDI " + e.Message.Command.ToString() +
                        " on channel " + (e.Message.MidiChannel + 1) + ", note: " +
                        e.Message.Data1 + "\n");
                }
                if ((e.Message.Command.ToString() == "NoteOn") &&
                    (e.Message.MidiChannel + 1 == nudMidiChannel.Value))
                {
                    if (e.Message.Data1 >= 48)
                    {
                        // 48 == C3
                        int iPlaylistItem = e.Message.Data1 - 48;
                        logMe("Playing playlist item #" + iPlaylistItem.ToString() + "\n");
                        //PlayPlayListItem(iPlaylistItem + 4);
                    }
                }
            }, null);
        }



        private void initMidi_Out()
        {
            if (OutputDevice.DeviceCount == 0)
            {
                MessageBox.Show("No MIDI Out devices available.", "შეცდომა!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Close();
            }
            else
            {
                try
                {


                    for (UInt16 i = 0; i < OutputDevice.DeviceCount; i++)
                    {

                        var modelOut = new OutputDevice(i);

                        logMe("\nId " + i.ToString() + "; " + modelOut.DeviceID.ToString() + " midi OUT device. Initializing MIDI interface... ");

                        modelOut.Dispose();
                    }


                    logMe("\nFound " + OutputDevice.DeviceCount.ToString() +
                        " midi OUT devices. Initializing MIDI interface... ");

                    //context = SynchronizationContext.Current;
                    OutDevice = new OutputDevice(1);
                    //OutDevice.ChannelMessageReceived += HandleChannelMessageReceived;
                    OutDevice.Error += new EventHandler<Sanford.Multimedia.ErrorEventArgs>(outDevice_Error);
                    //OutDevice..StartRecording();
                    logMe("Done!\n");
                }
                catch (Exception ex)
                {
                    logMe("Error initializing MIDI interface: " + ex.Message);
                    Close();
                }
            }
        }

        private void outDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            MessageBox.Show(e.Error.Message, "Error!",
                   MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        private void button12_Click(object sender, EventArgs e)
        {

            ChannelMessageBuilder builder = new ChannelMessageBuilder();

            builder.Command = ChannelCommand.NoteOn;
            builder.MidiChannel = 9;
            builder.Data1 = 60;
            builder.Data2 = 40;
            builder.Build();

            OutDevice.Send(builder.Result);



        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                casparClient.Connect(txtCasparServer.Text, int.Parse(txtCasparPort.Text));
                if (casparClient.Connected)
                {
                    lblStatus.Text = "CONNECTED";
                    lblStatus.ForeColor = Color.Green;
                    txtConsole.Text += Environment.NewLine + "Caspar სერვერი კავშირზეა! " + Environment.NewLine;
                }
                else
                {
                    lblStatus.Text = "NOT CONNECTED";
                    lblStatus.ForeColor = Color.Green;
                    txtConsole.Text += Environment.NewLine + "Caspar სერვერთან კავშირი არ არის! " + Environment.NewLine;
                }
            }
            catch (Exception)
            {
                txtConsole.Text += Environment.NewLine + "Caspar სერვერთან კავშირი ვერ მყარდება, არ მუშაობს ან არ გვიშვებს! " + Environment.NewLine;
            }
        }

        private void btnQ4_show_Click(object sender, EventArgs e)
        {
            if ((txtQ.Text == "") & (txtQ1.Text == ""))
            {
            }
            else
            { 
                TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_AnswersLayer.SelectedItem.ToString(), "REMOVE", 1, "");

                if (txtQ1.Text == "")
                    TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString() , cBox_QuestionLayer.SelectedItem.ToString(), "ADD", 1, "G" + cBox_AnsCount.SelectedItem.ToString());
                else
                    TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_QuestionLayer.SelectedItem.ToString(), "ADD", 1, "G" + cBox_AnsCount.SelectedItem.ToString() + "2");


                if (checkBox3.Checked)
                { 
                ChannelMessageBuilder MidiOutCmd = new ChannelMessageBuilder();

                    MidiOutCmd.Command = ChannelCommand.NoteOn;
                    MidiOutCmd.MidiChannel = Int32.Parse(cBoxMidiChannel.Text);
                        
                    MidiOutCmd.Data1 = Int32.Parse(cBoxMidiNote.Text);
                    MidiOutCmd.Data2 = 40;
                    MidiOutCmd.Build();

                OutDevice.Send(MidiOutCmd.Result);
                    
                }

            }
        }

        private void txtConsole_TextChanged(object sender, EventArgs e)
        {
           
                txtConsole.SelectionStart = txtConsole.TextLength;
                txtConsole.ScrollToCaret();
           
        }


        private void TellCGToShowGrfx(int _questNo, String _outPut, String _layer, String _cmd, uint _showParam, String _grfxName)
        {
            String _cmdStr = "";
            try
            {
                var reader = new StreamReader(casparClient.GetStream());
                var writer = new StreamWriter(casparClient.GetStream());

                _cmdStr = "CG ";
                _cmdStr += _outPut;
                _cmdStr += "-";
                _cmdStr += _layer;
                _cmdStr += " ";
                _cmdStr += _cmd;
                _cmdStr += " ";
                _cmdStr += _showParam.ToString();
                _cmdStr += " \"ASK/" + _grfxName + "\" 1 ";

                _cmdStr += CollectTemplateDataForCG(_questNo);

                writer.WriteLine(_cmdStr);
                writer.Flush();

                var reply = reader.ReadLine();

                txtConsole.Text += reply + Environment.NewLine;

                if (reply.Contains("201"))
                {
                    reply = reader.ReadLine();
                    txtConsole.Text += reply + Environment.NewLine;
                }
                else if (reply.Contains("200"))
                {
                    while (reply.Length > 0)
                    {
                        reply = reader.ReadLine();
                        txtConsole.Text += reply + Environment.NewLine;
                    }
                }

            }

            catch (Exception)
            {
                txtConsole.Text += "სერვერი კავშირის პრობლემა ან სხვა სახის პრობლემა. " + Environment.NewLine;
            }

        }



        private string CollecteDataIds(String ddataId, string vvalue)
        {
            String cmdStr = "";

            cmdStr = "<data id =\\\"" + ddataId + "\\\" value=\\\"" + vvalue + "\\\"/>";

            return cmdStr;
        }


        private string CollectTemplateDataForCG(int CCount)
        {
            String cmdStr = "";

            cmdStr += "\"<templateData>";
            var color = Color.Red;
            //const int val = 0x11223344;
            // var color = new Color(val);

            if (txtQ1.Text == "")
                cmdStr += "<componentData id=\\\"q0\\\">" + CollecteDataIds("text", txtQ.Text) + "</componentData>";

            else
            {
                cmdStr += "<componentData id=\\\"q0\\\">" + CollecteDataIds("text", txtQ.Text) + "</componentData>";
                cmdStr += "<componentData id=\\\"q1\\\">" + CollecteDataIds("text", txtQ1.Text) + "</componentData>";
            }



            if (CCount == 4)
            {
                //cmdStr += "<componentData id=\\\"f0\\\"><data id=\\\"text\\\" value=\\\"" + txtQ4.Text + "\\\"/></componentData>";

                //  cmdStr += "<componentData id=\\\"f0\\\" type=\"CasparTextField\">" + CollecteDataIds("text", txtQ4.Text) + CollecteDataIds("f0.textColor", String.Format("0x{0:X8}", color.ToArgb())) + "</componentData>";

             
                cmdStr += "<componentData id=\\\"a1\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_1.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a2\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_2.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a3\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_3.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a4\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_4.Text + "\\\"/></componentData>";
            }
            else if (CCount == 3)
            {
                //cmdStr += "<componentData id=\\\"f0\\\">" + CollecteDataIds("text", txtQ.Text) + "</componentData>";
                cmdStr += "<componentData id=\\\"a1\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_1.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a2\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_2.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a3\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_3.Text + "\\\"/></componentData>";

            }
            else if (CCount == 2)
            {
                //cmdStr += "<componentData id=\\\"f0\\\">" + CollecteDataIds("text", txtQ.Text) + "</componentData>";
                cmdStr += "<componentData id=\\\"a1\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_1.Text + "\\\"/></componentData>";
                cmdStr += "<componentData id=\\\"a2\\\"><data id=\\\"text\\\" value=\\\"" + txtAnsw4_2.Text + "\\\"/></componentData>";

            }
            else if (CCount == 0)
            {
                //cmdStr += "<componentData id=\\\"f0\\\">" + CollecteDataIds("text", txtQ.Text) + "</componentData>";
                cmdStr += "<componentData id=\\\"t0\\\"><data id=\\\"text\\\" value=\\\"" + txtBid.Text + "\\\"/></componentData>";
                

            }


            cmdStr += "</templateData>\"";




            return cmdStr;
        }

        
        private void btn4_1_correct_Click(object sender, EventArgs e)
        {
            if (txtQ.Text != "")
                TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_AnswersLayer.SelectedItem.ToString(), "ADD", 1, Q_4_CORRECT_Ans);
        }

        private void btn4_1_undo_Click(object sender, EventArgs e)
        {
            //TellCGToShowGrfx(4, 2, 11, "REMOVE", 1, "");
            TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_AnswersLayer.SelectedItem.ToString(), "REMOVE", 1, "");
        }

        private void rBtn_4_1_Click(object sender, EventArgs e)
        {
            if (rBtn_4_1.Checked)
                Q_4_CORRECT_Ans = "G" + cBox_AnsCount.SelectedItem.ToString() + " - 1ok";
        }

        private void rBtn_4_2_Click(object sender, EventArgs e)
        {
            if (rBtn_4_2.Checked)
                Q_4_CORRECT_Ans = "G" + cBox_AnsCount.SelectedItem.ToString() + " - 2ok";
        }

        private void rBtn_4_3_Click(object sender, EventArgs e)
        {
            if (rBtn_4_3.Checked)
                Q_4_CORRECT_Ans = "G" + cBox_AnsCount.SelectedItem.ToString() + " - 3ok";
        }

        private void rBtn_4_4_Click(object sender, EventArgs e)
        {
            if (rBtn_4_4.Checked)
                Q_4_CORRECT_Ans = "G" + cBox_AnsCount.SelectedItem.ToString() + " - 4ok";

        }

        private void PathsTabPage_Click(object sender, EventArgs e)
        {

        }

        private void button16_Click(object sender, EventArgs e)
        {
            TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_QuestionLayer.SelectedItem.ToString(), "REMOVE", 1, "");
            TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_AnswersLayer.SelectedItem.ToString(), "REMOVE", 1, "");
            TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_BidLayer.SelectedItem.ToString(), "REMOVE", 1, "");
        }

        private void rBtn4_4_Click(object sender, EventArgs e)
        {

        }

 

        private void cBox_AnsCount_SelectedIndexChanged(object sender, EventArgs e)
        {


            if (cBox_AnsCount.SelectedItem != null)      
            {
                rBtn_4_1.Checked = false;
                rBtn_4_2.Checked = false;
                rBtn_4_3.Checked = false;
                rBtn_4_4.Checked = false;

                if (cBox_AnsCount.SelectedItem.ToString() == "2")
                {
                    txtAnsw4_1.Enabled = true;
                    txtAnsw4_2.Enabled = true;
                    txtAnsw4_3.Enabled = false;
                    txtAnsw4_4.Enabled = false;
                    rBtn_4_1.Enabled = true;
                    rBtn_4_2.Enabled = true;
                    rBtn_4_3.Enabled = false;
                    rBtn_4_4.Enabled = false;
                }
                else if (cBox_AnsCount.SelectedItem.ToString() == "3")
                {
                    txtAnsw4_1.Enabled = true;
                    txtAnsw4_2.Enabled = true;
                    txtAnsw4_3.Enabled = true;
                    txtAnsw4_4.Enabled = false;
                    rBtn_4_1.Enabled = true;
                    rBtn_4_2.Enabled = true;
                    rBtn_4_3.Enabled = true;
                    rBtn_4_4.Enabled = false;

                }            
                else if (cBox_AnsCount.SelectedItem.ToString() == "4")
                {
                txtAnsw4_1.Enabled = true;
                txtAnsw4_2.Enabled = true;
                txtAnsw4_3.Enabled = true;
                txtAnsw4_4.Enabled = true;
                rBtn_4_1.Enabled = true;
                rBtn_4_2.Enabled = true;
                rBtn_4_3.Enabled = true;
                rBtn_4_4.Enabled = true;
                    

                }

        }

    }

        private void button15_Click(object sender, EventArgs e)
        {
            if ((txtBid.Text != ""))
            {
                TellCGToShowGrfx(0, (listBox1.SelectedIndex + 1).ToString(), cBox_BidLayer.SelectedItem.ToString(), "ADD", 1, "Tanxa0");
            }
            else
            { }

              
        }

        private void button17_Click(object sender, EventArgs e)
        {
            TellCGToShowGrfx(0, (listBox1.SelectedIndex + 1).ToString(), cBox_BidLayer.SelectedItem.ToString(), "REMOVE", 1, "");
        }
    }




}
