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

using System.Net;
using System.Net.Sockets;

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

        string currentLayer = "20";
        string currentChannel = "1";

        UInt16 nominalPosDelta = 80;

        string CGPrefixScoreBoard = "";
        string ChannelAndLayerOfScoreBoard = "";
        //string CGPrefixLowerTitleBoard = "";
        string CGPrefixBonusBoard = "";
        string ChannelAndLayerOfBonusBoard = "";

        string CGPrefixEliminateBoard = "";
        string ChannelAndLayerOfEliminateBoard = "";

        string CGPrefixLowerThirdBoard = "";
        string ChannelAndLayerOfLowerThirdBoard = "";


        string CGPrefixJudgesScoresBoard = "";
        string ChannelAndLayerOfJudgesScoresBoard = "";

        string CGPrefixJudgesScoresBoardFullFrame = "";
        string ChannelAndLayerOfJudgesScoresBoardFullFrame = "";



        public AsyncCallback pfnWorkerCallBack;
        public Socket m_mainSocket;
        public Socket[] m_workerSocket = new Socket[10];
        public int m_clientCount = 0;

        const int MAX_CLIENTS = 24;
        UInt16 JudgeTurn = 0;



        public class Contestant
        {
            public string ContItemID { get; set; }
            public string NameID { get; set; }
            public string ScoreID { get; set; }
            public int Position { get; set; }
            public string Gvari { get; set; }
            public int Score { get; set; }
            public string Phone { get; set; }
            public int PhoneCalls { get; set; }
            /*
             public Contestant(string _ItemID, string _ID, int _POS, string _Gvari, int _Score)
             {
                 this.ItemID = _ItemID;
                 this.ID = _ID;
                 this.Position = _POS;
                 this.Gvari = _Gvari;
                 this.Score = _Score;

             }
             */
        }

        public class Voters
        {
            public string JudgeIP { get; set; }
            public string JudgeName { get; set; }

            public Boolean Voted { get; set; }
        }

        public class PhoneAndPhoneCalls
        {
            public string Phone { get; set; }
            public UInt32 PhoneCalls { get; set; }

        }

        List<Contestant> ListContestant = new List<Contestant>();
        List<Contestant> ListContestantInStart = new List<Contestant>();
        BindingSource ContestantSource = new BindingSource();

        List<PhoneAndPhoneCalls> ListPhoneCalls = new List<PhoneAndPhoneCalls>();
        BindingSource PhoneAndPhoneCallsSource = new BindingSource();



        List<Voters> ListVoterJudges = new List<Voters>();


        string Q_4_CORRECT_Ans = "";
        string Q_3_CORRECT_Ans = "";
        string Q_2_CORRECT_Ans = "";

        //string ConfigFile = "C:\\CasparCG\\CasparCG Server\\server\\casparcg.config";
        string ConfigFile = "C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config";


        System.Net.Sockets.TcpClient casparClient = new System.Net.Sockets.TcpClient();





        public MainForm()
        {
            this.InitializeComponent();
        }




        public Boolean TellToCaspar(String CGCmd)
        {
            Boolean tmpBool = false;

            try
            {

                //logConsole.Text += CGCmd + Environment.NewLine + logConsole.Text + Environment.NewLine;
                txtConsole.Text = txtConsole.Text.Insert(0, CGCmd + Environment.NewLine);

                //txtConsole.Text += CGCmd + Environment.NewLine;

                var reader = new StreamReader(casparClient.GetStream());
                var writer = new StreamWriter(casparClient.GetStream());

                writer.WriteLine(CGCmd);
                writer.Flush();

                var reply = reader.ReadLine();

                //txtConsole.Text += reply + Environment.NewLine;
                //logConsole.Text = reply + logConsole.Text + Environment.NewLine;
                txtConsole.Text = txtConsole.Text.Insert(0, reply + Environment.NewLine);

                if (reply.Contains("201"))
                {
                    reply = reader.ReadLine();
                    //txtConsole.Text = reply + Environment.NewLine;
                    //logConsole.Text = reply + Environment.NewLine + logConsole.Text + Environment.NewLine;
                    txtConsole.Text = txtConsole.Text.Insert(0, reply + Environment.NewLine);
                    tmpBool = true;
                }
                else if (reply.Contains("200"))
                {
                    while (reply.Length > 0)
                    {
                        reply = reader.ReadLine();
                        //logConsole.Text = reply + Environment.NewLine + logConsole.Text + Environment.NewLine;
                        txtConsole.Text = txtConsole.Text.Insert(0, reply + Environment.NewLine);
                        tmpBool = true;
                    }
                }
            }
            catch (Exception)
            {
                txtConsole.Text = txtConsole.Text = txtConsole.Text.Insert(0, "სერვერი კავშირის პრობლემა ან სხვა სახის პრობლემა. " + Environment.NewLine);
                //                logConsole.Text += "სერვერი კავშირის პრობლემა ან სხვა სახის პრობლემა. " + Environment.NewLine + logConsole.Text + Environment.NewLine;
                tmpBool = false;

            }

            txtConsole.ScrollToCaret();
            return tmpBool;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            if (System.IO.File.Exists(ConfigFile))
                //if (System.IO.File.Exists("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config"))
                DeSerializeConfig(System.IO.File.ReadAllText(ConfigFile));
            //DeSerializeConfig(System.IO.File.ReadAllText("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config"));
            else
            {
                System.Windows.Forms.MessageBox.Show("A 'casparcg.config' file was not found in the same directory as this application.  One is now being generated.", "CasparCG Configurator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                SerializeConfig();
            }
            this.WireBindings();
            this.Updatechannel();
            this.SetToolTips();


            grp1.Enabled = false;

            // InitMidi_In();
            // initMidi_Out();


            //cBox_QuestionLayer.SelectedIndex = 0;
            //cBox_AnswersLayer.SelectedIndex = 0;



            ContestantSource.DataSource = ListContestant;
            ContestantDataGrid.DataSource = ContestantSource;
            ContestantDataGrid.ReadOnly = true;
            ContestantDataGrid.DefaultCellStyle.Font = new Font("Arial", 10);
            ContestantDataGrid.Columns[4].DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            ContestantDataGrid.Columns[5].DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            ContestantDataGrid.Columns[6].DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            //ContestantDataGrid.Columns[5].DefaultCellStyle.Font = new Font("Verdana", 12, FontStyle.Bold);



            PhoneAndPhoneCallsSource.DataSource = ListPhoneCalls;
            dataGridPhoneCalls.DataSource = PhoneAndPhoneCallsSource;
            dataGridPhoneCalls.ReadOnly = true;



            //            cBox_CGCurrChannel.Text = "2";
            //            cBox_CGCurrChannelLayer.Text = "5";



            cBox_CGScoreBoardCurrChannel.SelectedIndex = 0;
            cBox_CGCurrScoreBoardChannelLayer.SelectedIndex = 0;

            cBox_CGCurrLowerTitleChannel.SelectedIndex = 0;
            cBox_CGCurrLowerTitleChannelLayer.SelectedIndex = 0; ;

            cBox_CGJudgesScoreChannel.SelectedIndex = 0;
            cBox_CGJudgesScoreChannelLayer.SelectedIndex = 0;



            /*
            CsomedataArr.ListContestant[0].ItemID = "contItem1";
            ListContestant[0].ID = "_NameCont1";
            ListContestant[0].Position = 1;
            ListContestant[0].Gvari = "უპირველესი მომღერალი";
            ListContestant[0].Score = 0;
            */


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
            var extraTypes = new Type[1] { typeof(AbstractConsumer) };

            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using (var writer = doc.CreateWriter())
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

            //C:\CasparCG\CasparCG Server\server
            //using (var writer = new XmlTextWriter("C:\\CasparCG\\CasparCG Server\\server\\casparcg.config", new UTF8Encoding(false, false))) // No BOM

            //using (var writer = new XmlTextWriter("C:\\CasparCG\\CasparCG Server 2.1.0\\CasparCG Server\\server\\casparcg.config", new UTF8Encoding(false, false))) // No BOM
            using (var writer = new XmlTextWriter(ConfigFile, new UTF8Encoding(false, false))) // No BOM
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
                    System.Windows.Forms.MessageBox.Show("There was an error reading the current 'casparcg.config' file.  A new one will be generated.", "CasparCG Configurator", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        this.consumerEditorControl = new DecklinkConsumerControl(listBox2.SelectedItem as DecklinkConsumer, config.AvailableDecklinkIDs);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(ScreenConsumer))
                    {
                        this.consumerEditorControl = new ScreenConsumerControl(listBox2.SelectedItem as ScreenConsumer);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(BluefishConsumer))
                    {
                        this.consumerEditorControl = new BluefishConsumerControl(listBox2.SelectedItem as BluefishConsumer, config.AvailableBluefishIDs);
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
            else if (res == System.Windows.Forms.DialogResult.Cancel)
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

        private void btnQ4_show_Click(object sender, EventArgs e)
        {
            if ((txtQ.Text == "") & (txtQ1.Text == ""))
            {
            }
            else
            {
                TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_AnswersLayer.SelectedItem.ToString(), "REMOVE", 1, "");

                if (txtQ1.Text == "")
                    TellCGToShowGrfx(4, (listBox1.SelectedIndex + 1).ToString(), cBox_QuestionLayer.SelectedItem.ToString(), "ADD", 1, "G" + cBox_AnsCount.SelectedItem.ToString());
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

        private void btnSendCommand_Click(object sender, EventArgs e)
        {
            try
            {
                var reader = new StreamReader(casparClient.GetStream());
                var writer = new StreamWriter(casparClient.GetStream());

                writer.WriteLine(txtCmdPrefix.Text);
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

        private void button29_Click(object sender, EventArgs e)
        {
            //TellToCaspar("CG 1-" + currentLayer + " ADD 1 \"html/DidiScena/Z2\" 0");



        }

        private void button30_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //lblCurrCont.Text = e.RowIndex.ToString() + " - " + e.ColumnIndex.ToString() + " " +  dataGridView1.Rows[e.RowIndex].Cells[0].Value;
        }
        /*
        private void button31_Click(object sender, EventArgs e)
        {
            var ContestantObj = new Contestant();

            if (ListContestant.Count() < Convert.ToInt16(txtContestantCount.Text))
            {
                

               

                ContestantObj.ContItemID = "contItem" + (ListContestant.Count() + 1).ToString();
                ContestantObj.NameID = "_NameCont" + (ListContestant.Count() + 1).ToString();
                ContestantObj.ScoreID = "_ScoreCont" + (ListContestant.Count() + 1).ToString();
                ContestantObj.Position = ListContestant.Count() + 1;
                ContestantObj.Gvari = textBox8.Text;
                ContestantObj.Score = 0;


                ListContestant.Add(ContestantObj);

                textBox8.Text = "";







                ContestantSource.ResetBindings(false);

        
            }

            
        }
        */
        private void button32_Click(object sender, EventArgs e)
        {


        }

        /*
        private void button35_Click(object sender, EventArgs e)
        {
            var locCMD = "";
            var locCouple = "";
            locCMD ="CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"updateScore(";

            

                        locCouple = "'" + ListContestant[comboBox13.SelectedIndex].ScoreID + "'" + "," + txtScoreToBeAdded.Text;
            //locCMD += "\"_NameCont" + i.ToString() + "\"" + ":" + "\"" + ListContestant[i].Gvari + "\"" + "," + "\"_ScoreCont" + i.ToString() + "\"" + ":" + "\"" + ListContestant[i].Score.ToString() + "\"";
            locCMD += locCouple;


            //locCMD += "}\"";
            //locCMD += "}";
            locCMD += ")\"";

            //CG 1 - 20 UPDATE 1 "{\"f0\":\"15\",\"f1\":\"85\"}"


            TellToCaspar(locCMD);

            ListContestant[comboBox13.SelectedIndex].Score += Convert.ToInt16(txtScoreToBeAdded.Text);

            ContestantSource.ResetBindings(false);
        }
        */
        /*
        private void button36_Click(object sender, EventArgs e)
        {

            
            bool swapped = false;

            void swap(int n, int n1)
            {
                int locn = n;
                int locn1 = n1;

                int locnPos = ListContestant[n].Position;
                int locn1Pos = ListContestant[n1].Position;

                ListContestant[locn].Position = locn1Pos;
                ListContestant[locn1].Position = locnPos;
            }

        

            for (UInt16 i=0; i< Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                swapped = false;
                for (int j = (Convert.ToInt16(txtContestantCount.Text) - 1); j >= 1; j--)
                {
                    if (ListContestant[comboBox13.SelectedIndex].Score > ListContestant[j-1].Score)
                    {
                        swap(j, j - 1);
                        swapped = true;
                        j--;
                    }
                }
            }

            Contestantsource.ResetBindings(false);


        }
        */
        private void button37_Click(object sender, EventArgs e)
        {
            //int prePos = ListContestant[comboBox13.SelectedIndex].Position;

            //ListContestant.Sort()


        }

        private void cBox_CGCurrChannel_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cBox_CGCurrChannelLayer_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {

            if (cBoxAMPCcmd.SelectedItem.ToString() == "INVOKE")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"" + txtCmdBody.Text + "\"");
            else if (cBoxAMPCcmd.SelectedItem.ToString() == "ADD & PLAY")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ADD " + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " \"html/DidiScena/" + txtCmdBody.Text + "\" 1");
            else if (cBoxAMPCcmd.SelectedItem.ToString() == "ADD")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ADD " + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " \"html/DidiScena/" + txtCmdBody.Text + "\" 0");



        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }




        private SortOrder getSortOrder(int columnIndex)
        {
            if (ContestantDataGrid.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
                ContestantDataGrid.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.Descending)
            {
                ContestantDataGrid.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                return SortOrder.Ascending;
            }
            else
            {
                ContestantDataGrid.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;
                return SortOrder.Descending;
            }
        }

        private SortOrder getSortOrderPhoneCall(int columnIndex)
        {
            if (dataGridPhoneCalls.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
                dataGridPhoneCalls.Columns[columnIndex].HeaderCell.SortGlyphDirection == SortOrder.Descending)
            {
                dataGridPhoneCalls.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                return SortOrder.Ascending;
            }
            else
            {
                dataGridPhoneCalls.Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;
                return SortOrder.Descending;
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {





        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {


        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button40_Click(object sender, EventArgs e)
        {
            TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"Inv_AllIn0\"");
        }

        private void button41_Click(object sender, EventArgs e)
        {
            TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"allIn0\"");
        }

        private void cBox_CGCurrChannel_Click(object sender, EventArgs e)
        {
            txtCmdPrefix.Text = "CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ";
        }

        private void cBox_CGCurrChannelLayer_Click(object sender, EventArgs e)
        {
            txtCmdPrefix.Text = "CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ";
        }

        private void button29_Click_1(object sender, EventArgs e)
        {

        }

        private void button30_Click_1(object sender, EventArgs e)
        {
        }

        private void button42_Click(object sender, EventArgs e)
        {



        }

        private void btnLoadFrom_Click(object sender, EventArgs e)
        {

            string[] momgerali = new string[Convert.ToUInt16(txtContestantCount.Text)];
            string[] phoneNum = new string[Convert.ToUInt16(txtContestantCount.Text)];


            ListContestant.Clear();
            ListContestantInStart.Clear();
            //ListContestant.RemoveAll();
            ListVoterJudges.Clear();
            ContestantSource.Clear();
            //ContestantDataGrid.Rows.Clear();
            //dataGridView1.Rows.Clear();


            ChannelAndLayerOfScoreBoard = cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfBonusBoard = cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrScoreBoardChannelLayer.Text) + 1).ToString();
            ChannelAndLayerOfEliminateBoard = cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrScoreBoardChannelLayer.Text) + 2).ToString();
            ChannelAndLayerOfLowerThirdBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + cBox_CGCurrLowerTitleChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfJudgesScoresBoardFullFrame = cBox_CGJudgesScoreChannel.SelectedItem.ToString() + "-" + cBox_CGJudgesScoreChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfJudgesScoresBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrLowerTitleChannelLayer.Text) + 2).ToString();


            TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);
            TellToCaspar("CLEAR " + ChannelAndLayerOfBonusBoard);
            TellToCaspar("CLEAR " + ChannelAndLayerOfEliminateBoard);
            TellToCaspar("CLEAR " + ChannelAndLayerOfLowerThirdBoard);
            TellToCaspar("CLEAR " + ChannelAndLayerOfJudgesScoresBoardFullFrame);
            TellToCaspar("CLEAR " + ChannelAndLayerOfJudgesScoresBoard);


            CGPrefixScoreBoard = "CG " + ChannelAndLayerOfScoreBoard;
            CGPrefixBonusBoard = "CG " + ChannelAndLayerOfBonusBoard;
            CGPrefixEliminateBoard = "CG " + ChannelAndLayerOfEliminateBoard;
            CGPrefixLowerThirdBoard = "CG " + ChannelAndLayerOfLowerThirdBoard;
            CGPrefixJudgesScoresBoard = "CG " + ChannelAndLayerOfJudgesScoresBoard;
            CGPrefixJudgesScoresBoardFullFrame = "CG " + ChannelAndLayerOfJudgesScoresBoardFullFrame;





            var fileStream = new FileStream(@"c:\CGData\bonuses.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };

                while ((line = streamReader.ReadLine()) != null)
                {
                    lineSplited = line.Split(',');
                    //                    var BonusButtonObj = new Voters();
                    btnBonus_0.Text = lineSplited[0];
                    btnBonus_1.Text = lineSplited[1];
                    btnBonus_2.Text = lineSplited[2];
                    btnBonus_3.Text = lineSplited[3];
                    btnBonus_4.Text = lineSplited[4];
                    btnBonus_5.Text = lineSplited[5];
                    btnBonus_6.Text = lineSplited[6];
                    btnBonus_7.Text = lineSplited[7];
                    btnBonus_8.Text = lineSplited[8];
                    btnBonus_9.Text = lineSplited[9];

                }

            }






            fileStream = new FileStream(@"c:\CGData\judges.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };

                while ((line = streamReader.ReadLine()) != null)
                {
                    lineSplited = line.Split(',');
                    var VoterObj = new Voters();
                    VoterObj.JudgeIP = lineSplited[0];
                    VoterObj.JudgeName = lineSplited[1];
                    VoterObj.Voted = false;
                    ListVoterJudges.Add(VoterObj);

                }

            }




            grBoxJiuri0.Text = ListVoterJudges[0].JudgeName + ":" + ListVoterJudges[0].JudgeIP;
            txt_IPJiuri0.Text = ListVoterJudges[0].JudgeIP;
            grBoxJiuri1.Text = ListVoterJudges[1].JudgeName + ":" + ListVoterJudges[1].JudgeIP;
            txt_IPJiuri1.Text = ListVoterJudges[1].JudgeIP;
            grBoxJiuri2.Text = ListVoterJudges[2].JudgeName + ":" + ListVoterJudges[2].JudgeIP;
            txt_IPJiuri2.Text = ListVoterJudges[2].JudgeIP;
            grBoxJiuri3.Text = ListVoterJudges[3].JudgeName + ":" + ListVoterJudges[3].JudgeIP;
            txt_IPJiuri3.Text = ListVoterJudges[3].JudgeIP;
            grBoxJiuri4.Text = ListVoterJudges[4].JudgeName + ":" + ListVoterJudges[4].JudgeIP;
            

            fileStream = new FileStream(@"c:\CGData\contestants.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };
                UInt16 i = 0;
                while (((line = streamReader.ReadLine()) != null) && (i < Convert.ToUInt16(txtContestantCount.Text)))
                {
                    lineSplited = line.Split(',');
                    phoneNum[i] = lineSplited[0];
                    momgerali[i] = lineSplited[1];
                    i++;

                    //var PhoneCallObj = new PhoneAndPhoneCalls();
                    //PhoneCallObj.Phone = lineSplited[0];
                    //PhoneCallObj.PhoneCalls = Convert.ToUInt32(lineSplited[1]);
                    //ListPhoneCalls.Add(PhoneCallObj);

                }


            }





            //momgerali[0] = "John Lenon";
            //momgerali[1] = "ნატო მეტონიძე";
            //momgerali[2] = "დონალდ ტრამპი";
            //momgerali[3] = "სინატრა";
            //momgerali[4] = "ვიღაცა მომღერალი";
            //           momgerali[5] = "პაპუნა ჩადუნელი";
            //           momgerali[6] = "ბიძინა ივანიშვილი";
            //           momgerali[7] = "ნანა ბელქანია";
            //           momgerali[8] = "ალ ბანო";
            //           momgerali[9] = "jeckson";

            /*            
                       phoneNum[1] = "99510000002";
                        phoneNum[2] = "99510000003";
                        phoneNum[3] = "99510000004";
                        phoneNum[4] = "99510000005";
                        phoneNum[5] = "99510000006";
                        phoneNum[6] = "99510000007";
                        phoneNum[7] = "99510000008";
                        phoneNum[8] = "99510000009";
                        phoneNum[9] = "99510000010";
            */

            //for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            //{
            //    phoneNum[i] = "995100000" + i.ToString("00");

            //    //momgerali[i] = "მომღერალი" + i.ToString("00"); 
            //}





            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                var ContestantObj = new Contestant();
                ContestantObj.ContItemID = "contItem" + i.ToString("00");
                ContestantObj.NameID = "_NameCont" + i.ToString("00");
                ContestantObj.ScoreID = "_ScoreCont" + i.ToString("00");
                ContestantObj.Position = i + 1;
                //ContestantObj.Position = i ;
                ContestantObj.Gvari = momgerali[i];
                ContestantObj.Phone = phoneNum[i];
                ContestantObj.Score = 0;
                //ContestantObj.Score = 100 - (i * 10);
                ContestantObj.PhoneCalls = 0;
                ListContestant.Add(ContestantObj);
                ListContestantInStart.Add(ContestantObj);


            }



            ContestantSource.ResetBindings(true);
            ContestantDataGrid.Columns[ContestantDataGrid.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        }

        private void button34_Click(object sender, EventArgs e)
        {
            //TellToCaspar("CG 1-" + currentLayer + " INVOKE 0 \"Inv_AllIn\"");
            var locCMD = "";
            var locCouple = "";
            locCMD = "CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"Inv_update2(";




            //CG 1 - 20 INVOKE 0 "Inv_update2(45,'abs','d','daa')"

            //locCMD = "CG 1-" + currentLayer + " UPDATE 0 \"{" ;

            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                locCouple = "'" + ListContestant[i].Gvari + "'" + "," + ListContestant[i].Score.ToString();
                //locCMD += "\"_NameCont" + i.ToString() + "\"" + ":" + "\"" + ListContestant[i].Gvari + "\"" + "," + "\"_ScoreCont" + i.ToString() + "\"" + ":" + "\"" + ListContestant[i].Score.ToString() + "\"";
                locCMD += locCouple;
                if (i != 9)
                    locCMD += ",";
            }

            //locCMD += "}\"";
            //locCMD += "}";
            locCMD += ")\"";

            //CG 1 - 20 UPDATE 1 "{\"f0\":\"15\",\"f1\":\"85\"}"


            TellToCaspar(locCMD);

            ContestantSource.ResetBindings(false);

        }

        private void doPLUS_forContestant(int PhoneCalls)
        {
            int currContestantPos = ContestantDataGrid.SelectedRows[0].Index;
            int currContestantOldPos = currContestantPos;
            int currContestantNewScore = ListContestant[currContestantPos].Score + Convert.ToInt16(txtScoreToBeAdded.Text);
            int currContestantNewPosition = currContestantPos;
            //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();

            int PosDeltaIfEqual = 0;



            Console.WriteLine("currContestantPos = dataGridView1.CurrentRow.Index  =" + currContestantPos);
            Console.WriteLine("dataGridView1.Rows[currContestantPos].Cells[\"ContItemID\"].Value = " +
            ContestantDataGrid.Rows[currContestantPos].Cells["ContItemID"].Value);

            Console.WriteLine("currContestantNewScore = ListContestant[currContestantPos].Score + Convert.ToInt16(txtScoreToBeAdded.Text) = " + currContestantNewScore);

            String itemTokens = "";
            for (int i = 0; i < currContestantPos; i++)
            {
                Console.WriteLine("Now Check with  " + ListContestant[i].ContItemID);
                if (currContestantNewScore > ListContestant[i].Score)
                {
                    Console.WriteLine(currContestantNewScore + " > " + ListContestant[i].Score + " of " + ListContestant[i].ContItemID);
                    itemTokens = itemTokens + "'" + ListContestant[i].ContItemID + "',";
                    currContestantNewPosition--;
                }
                else if (currContestantNewScore == ListContestant[i].Score)
                {
                    Console.WriteLine(currContestantNewScore + " = " + ListContestant[i].Score + " of " + ListContestant[i].ContItemID);
                    if (PhoneCalls > ListContestant[i].PhoneCalls)
                    {
                        Console.WriteLine(PhoneCalls + " > " + ListContestant[i].PhoneCalls + " of " + ListContestant[i].ContItemID);
                        itemTokens = itemTokens + "'" + ListContestant[i].ContItemID + "',";
                        currContestantNewPosition--;

                        //ListContestant[currContestantPos].Position = ListContestant[i].Position;
                        //PosDeltaIfEqual++;
                        // currContestantNewPosition--;

                    }


                }
            }

            if (itemTokens.EndsWith(","))
                itemTokens = itemTokens.Substring(0, itemTokens.Length - 1);

            Console.WriteLine("itemokensText " + itemTokens);
            Console.WriteLine("Position of  " + ListContestant[ContestantDataGrid.CurrentRow.Index].ContItemID +
                             " before Adding " + txtScoreToBeAdded.Text + " score was " + (currContestantOldPos + 1).ToString() + ", and " +
                             " After will be " + (currContestantNewPosition + 1).ToString() + " with " + currContestantNewScore.ToString() + " score");



            txtCmdBody.Text = "reArrangeItemsFromTo_NEW('" + ListContestant[currContestantPos].ScoreID + "'," + txtScoreToBeAdded.Text +
    ",'#" + ListContestant[currContestantPos].ContItemID + "','" + (currContestantOldPos - currContestantNewPosition).ToString() + "',";// + ")" ;

            txtCmdBody.Text = txtCmdBody.Text + itemTokens;

            if (txtCmdBody.Text.EndsWith(","))
                txtCmdBody.Text = txtCmdBody.Text.Substring(0, txtCmdBody.Text.Length - 1);

            txtCmdBody.Text = txtCmdBody.Text + ")";


            if ((currContestantOldPos - currContestantNewPosition) == 0)
            {
                if (TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"updateScore('" + ListContestant[currContestantPos].ScoreID + "','" + txtScoreToBeAdded.Text + "')\""))

                {
                    ListContestant[currContestantPos].Score = currContestantNewScore;
                    ListContestant[currContestantPos].PhoneCalls = PhoneCalls;

                    //ListContestant[currContestantPos].Position = currContestantNewPosition + 1;

                    ContestantSource.ResetBindings(true);
                }

            }
            else
            {
                if (TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\""))
                //                    if (TellToCaspar("CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"updateScore('"+ ListContestant[currContestantPos].ScoreID + "','" + txtScoreToBeAdded.Text + "')\""))
                {
                    ListContestant[currContestantPos].Score = currContestantNewScore;
                    ListContestant[currContestantPos].PhoneCalls = PhoneCalls;
                    //ListContestant[currContestantPos].Position = currContestantNewPosition;
                    ListContestant[currContestantPos].Position = currContestantNewPosition + 1;


                    //string strColumnName = "Position";
                    //SortOrder strSortOrder = getSortOrder(3);

                    string strColumnName = "Score";
                    SortOrder strSortOrder = getSortOrder(5);


                    //ListContestant = ListContestant.OrderBy(x => typeof(Contestant).GetProperty(strColumnName).GetValue(x, null)).ToList();
                    ListContestant = ListContestant.OrderByDescending(x => typeof(Contestant).GetProperty("Score").GetValue(x, null)).ThenByDescending(x => x.PhoneCalls).ToList();
                    ContestantDataGrid.DataSource = ListContestant;
                    ContestantDataGrid.Columns[5].HeaderCell.SortGlyphDirection = strSortOrder;

                    ContestantSource.ResetBindings(true);


                    for (int i = 0; i < ListContestant.Count; i++)
                    {
                        //if (currContestantPos != i)
                        ListContestant[i].Position = i + 1;
                    }


                    /*
                    if (PhoneCalls == 0)
                    {
                        for (int i = 0; i < ListContestant.Count; i++)
                        {
                            //if (currContestantPos != i)
                            ListContestant[i].Position = i + 1;
                        }
                    }
                    else
                    {
                        //string strColumnName = "Score";
                        strSortOrder = getSortOrder(3);
                        //strSortOrder = SortOrder.Ascending; ;
                        
                        ListContestant = ListContestant.OrderBy(x => typeof(Contestant).GetProperty("Position").GetValue(x, null)).ToList();
                        ContestantDataGrid.DataSource = ListContestant;
                        ContestantDataGrid.Columns[4].HeaderCell.SortGlyphDirection = strSortOrder;

                        ContestantSource.ResetBindings(true);

                    }
                    */



                    //for (int i = 0; i < ListContestant.Count; i++)
                    //{
                    //    if (currContestantPos != i)
                    //    ListContestant[i].Position = ListContestant[i].Position + 1;
                    //}


                    ContestantSource.ResetBindings(true);


                }



            }

            txtScoreToBeAdded.Text = "0";
            lblCurrContestant.ForeColor = Color.Red; ;
            lblCurrContestant.Text = "აარჩიე მომღერალი";
            btnPLUS.Enabled = false;
            btnPLUS.BackColor = SystemColors.ButtonFace;
            btnUpdateFromJudges.BackColor = Color.LightGray;


            txtJudgeValuesToZero();
        }


        public void txtJudgeValuesToZero()
        {
            txtJudgeValue0.Text = "0";
            txtJudgeValue1.Text = "0";
            txtJudgeValue2.Text = "0";
            txtJudgeValue3.Text = "0";
            txtJudgeValue4.Text = "0";

        }

        private void btnPLUS_Click(object sender, EventArgs e)
        {
            string UpPrefix = "-=";
            string DownPrefix = "+=";
            //int currContestantPos = dataGridView1.CurrentRow.Index;

            if ((ContestantDataGrid.Rows.Count <= 1) || (Convert.ToUInt16(txtScoreToBeAdded.Text) == 0) || (lblCurrContestant.Text == "აარჩიე მომღერალი") )
            {
                return;
            }
            else
            {
                doPLUS_forContestant(0);

                btnVoteStandBy.PerformClick();
                if (cBox_AutoTitleOnOFF.Checked)
                {
                    TitleInOut();
                }
                btn_title_InOut.Enabled = false;
            }


        }

        private void btbBoxesIn_Click(object sender, EventArgs e)
        {
            //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 1).ToString();

            Button[] barray = { btnBonus_0, btnBonus_1, btnBonus_2, btnBonus_3, btnBonus_4, btnBonus_5, btnBonus_6, btnBonus_7, btnBonus_8, btnBonus_9 };

            if (btbBoxesIn.Text == "BOXES IN")
            { 
                TellToCaspar(CGPrefixBonusBoard + " ADD 1 \"html/DidiScena/bbox\" 0");
                TellToCaspar(CGPrefixBonusBoard + " INVOKE 0 \"" + "createBonusBoxItems(" + txtContestantCount.Text + ")" + "\"");            
                TellToCaspar(CGPrefixBonusBoard + " INVOKE 0 \"" + "go1" + "\"");

            //Button nBtn = new Button();
                


                for (UInt16 i=0; i< Convert.ToUInt16(txtContestantCount.Text); i++)
                    { 
                        barray[i].Enabled = true;
                    }

                btbBoxesIn.Text = "BOXES OUT";
            }
            else
            {
                TellToCaspar("CLEAR " + ChannelAndLayerOfBonusBoard);
                for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
                {
                    barray[i].Enabled = false;
                }                
                btbBoxesIn.Text = "BOXES IN";
            }




        }

        private void dataGridPhoneCalls_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string strColumnName = dataGridPhoneCalls.Columns[e.ColumnIndex].Name;
            SortOrder strSortOrder = getSortOrderPhoneCall(e.ColumnIndex);


            if (strSortOrder == SortOrder.Ascending)
            {
                ListPhoneCalls = ListPhoneCalls.OrderBy(x => typeof(PhoneAndPhoneCalls).GetProperty(strColumnName).GetValue(x, null)).ToList();
            }
            else
            {
                ListPhoneCalls = ListPhoneCalls.OrderByDescending(x => typeof(PhoneAndPhoneCalls).GetProperty(strColumnName).GetValue(x, null)).ToList();
            }
            dataGridPhoneCalls.DataSource = ListPhoneCalls;
            dataGridPhoneCalls.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = strSortOrder;

        }

        public void rendomizePhoneCals()
        {
            Random r = new Random();

            using (System.IO.StreamWriter file =  new System.IO.StreamWriter(@"c:\CGData\data.txt"))
            {
                foreach (Contestant Cline in ListContestant)
                {
                    // If the line doesn't contain the word 'Second', write the line to the file.

                    file.WriteLine(Cline.Phone + "," + (r.Next(15000)).ToString());
                } 
                                   
            }

        }


        private void btnCallsImport_Click(object sender, EventArgs e)
        {
            ListPhoneCalls.Clear();


            //rendomizePhoneCals();

            var fileStream = new FileStream(@"c:\CGData\data.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };
                while ((line = streamReader.ReadLine()) != null)
                {


                    lineSplited = line.Split('\t');
                    for (UInt16 i = 0; i < ListContestant.Count; i++)
                    {
                        if (lineSplited[0] == ListContestant[i].Phone)
                        {
                            var PhoneCallObj = new PhoneAndPhoneCalls();
                            PhoneCallObj.Phone = lineSplited[0];
                            PhoneCallObj.PhoneCalls = Convert.ToUInt32(lineSplited[1]);
                            ListPhoneCalls.Add(PhoneCallObj);
                            break;
                        }
                    }

                }


            }


            PhoneAndPhoneCallsSource.ResetBindings(false);

            string strColumnName = "PhoneCalls";
            //SortOrder strSortOrder = getSortOrderPhoneCall(1);

            SortOrder strSortOrder = SortOrder.Descending;

            ListPhoneCalls = ListPhoneCalls.OrderByDescending(x => typeof(PhoneAndPhoneCalls).GetProperty(strColumnName).GetValue(x, null)).ToList();

            //if (strSortOrder == SortOrder.Ascending)
            //{
            //    ListContestant = ListContestant.OrderBy(x => typeof(Contestant).GetProperty(strColumnName).GetValue(x, null)).ToList();
            //}
            //else
            //{
            //    ListContestant = ListContestant.OrderByDescending(x => typeof(Contestant).GetProperty(strColumnName).GetValue(x, null)).ToList();
            //}
            dataGridPhoneCalls.DataSource = ListPhoneCalls;
            dataGridPhoneCalls.Columns[1].HeaderCell.SortGlyphDirection = strSortOrder;



            //for (int i = 0; i < ListContestant.Count; i++)
            //{
            //    ListContestant[i].Position = i + 1;
            //}
            //Contestantsource.ResetBindings(true);


            PhoneAndPhoneCallsSource.ResetBindings(true);
            //ListPhoneCalls.Sort();

            //UInt16 i = 0;
            //foreach (DataGridViewRow PhoneCalls in dataGridPhoneCalls.Rows)
            //{
            //rowPhone.Cells[0].Value.ToString();
            //}

            setAllScoresWhite();
        }

        private void btnBonus_X_Click(object sender, EventArgs e)
        {
            Int32 CurrRowIndex = Convert.ToInt16(((sender as Button).Name).Split('_')[1]);
            Int32 CurrRowIndexInGrid = -1;

            dataGridPhoneCalls.ClearSelection();
            dataGridPhoneCalls.Rows[CurrRowIndex].Selected = true;

            txtScoreToBeAdded.Text = (sender as Button).Text;// "50";
            //int currRow = -1;

            string searchValue = dataGridPhoneCalls.Rows[CurrRowIndex].Cells[0].Value.ToString();
            int phoneCallValue = Convert.ToInt32(dataGridPhoneCalls.Rows[CurrRowIndex].Cells[1].Value.ToString());

            ContestantDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ContestantDataGrid.ClearSelection();


            try
            {
                foreach (DataGridViewRow row in ContestantDataGrid.Rows)
                {
                    if (row.Cells[6].Value.ToString().Equals(searchValue))
                    {
                        row.Selected = true;
                        CurrRowIndexInGrid = row.Index;
                        ListContestant[CurrRowIndexInGrid].PhoneCalls = phoneCallValue;
                        break;
                    }
                }


                lblCurrCont.Text = CurrRowIndexInGrid.ToString("00") + " - " + ContestantDataGrid.Rows[CurrRowIndexInGrid].Cells[0].Value;
                lblCurrContestant.Text = ListContestant[CurrRowIndexInGrid].Gvari;



                TellToCaspar(CGPrefixBonusBoard + " INVOKE 0 \"" + "bonusBoxGoAway('#bbox" + CurrRowIndex.ToString("00") + "'" +
                    "" +
                    ")" + "\"");


                doPLUS_forContestant(phoneCallValue);
                //btnPLUS.PerformClick();

                (sender as Button).Enabled = false;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
                CurrRowIndex = -1;
            }


        }
        private void btnBonus1_Click(object sender, EventArgs e)
        {

        }

        private void button38_Click(object sender, EventArgs e)
        {

        }



        private void JujgeButtonClick(object sender, EventArgs e)
        {
            if (((sender as Button).Name).Split('_')[2] == "0")
                txtJudgeValue0.Text = (sender as Button).Text;
            else if (((sender as Button).Name).Split('_')[2] == "1")
                txtJudgeValue1.Text = (sender as Button).Text;
            else if (((sender as Button).Name).Split('_')[2] == "2")
                txtJudgeValue2.Text = (sender as Button).Text;
            else if (((sender as Button).Name).Split('_')[2] == "3")
                txtJudgeValue3.Text = (sender as Button).Text;
            else if (((sender as Button).Name).Split('_')[2] == "4")
                txtJudgeValue4.Text = (sender as Button).Text;


            //            txtScoreToBeAdded.Text = (Convert.ToUInt16(txtScoreToBeAdded.Text) + Convert.ToUInt16((sender as Button).Text)).ToString(); 
        }

        private void button35_Click(object sender, EventArgs e)
        {
            JujgeButtonClick(sender, e);
        }

        private void btnBonus2_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus3_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus4_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus5_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus6_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus7_Click(object sender, EventArgs e)
        {

        }

        private void btnConnectCG_Click(object sender, EventArgs e)
        {
            try
            {
                casparClient.Connect(txtCasparServer.Text, int.Parse(txtCasparPort.Text));
                if (casparClient.Connected)
                {
                    lblStatus.Text = "CONNECTED";
                    lblStatus.ForeColor = Color.Green;
                    txtConsole.Text += Environment.NewLine + "Caspar სერვერი კავშირზეა! " + Environment.NewLine;
                    txt_IP.Text = TcpVoting.GetIP();
                    txt_Port.Text = "8818";
                    txtConsole.Text += Environment.NewLine + "Voting Server is Up ! " + txt_IP.Text + ":" + txt_Port.Text + Environment.NewLine;


                    grBoxJiuri0.ForeColor = Color.Black;
                    grBoxJiuri1.ForeColor = Color.Black;
                    grBoxJiuri2.ForeColor = Color.Black;
                    grBoxJiuri3.ForeColor = Color.Black;
                    grBoxJiuri4.ForeColor = Color.Black;

                    grp1.Enabled = true;
                    grpBonus.Enabled=true;

                    pnl_EliminateTasks.Enabled = true;

                }
                else
                {
                    lblStatus.Text = "NOT CONNECTED";
                    lblStatus.ForeColor = Color.Green;
                    txtConsole.Text += Environment.NewLine + "Caspar სერვერთან კავშირი არ არის! " + Environment.NewLine;
                    grp1.Enabled = false;
                    grpBonus.Enabled = false;
                    pnl_EliminateTasks.Enabled = false;
                }
            }
            catch (Exception)
            {
                txtConsole.Text += Environment.NewLine + "Caspar სერვერთან კავშირი ვერ მყარდება, არ მუშაობს ან არ გვიშვებს! " + Environment.NewLine;
            }
        }

        private void btnAllIn_Click(object sender, EventArgs e)
        {
            string tmpStr = "";
            //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();
            //txtCmdBody.Text = "Inv_AllIn(";
            txtCmdBody.Text = "allIn(";



            ChannelAndLayerOfScoreBoard = cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString();
            CGPrefixScoreBoard = "CG " + ChannelAndLayerOfScoreBoard;





            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;
                tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[0].Value + "'";//"'," + dataGridView1.Rows[i].Cells[5].Value;
                if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                    tmpStr = tmpStr + ",";

            }





            txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";
            TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");


            //TellToCaspar("CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"Inv_AllIn\"");


            //            txtCmdBody.Text = "reArrangeItemsFromTo1('" + ListContestant[currContestantPos].ScoreID + "'," + currContestantNewScore.ToString() +
            //                ",'#" + ListContestant[currContestantPos].ContItemID + "','" + UpPrefix + (currContestantDeltaPosition * nominalPosDelta).ToString() + "'" + ")";





        }

        private void btnLoadScene_Click(object sender, EventArgs e)
        {
            if (btnLoadScene.Text == "Load ScoreBoard Scene")
            {
                btnLoadFrom.PerformClick();

                string tmpStr = "";
                string bgfile = "BigSceneBg2";
                //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();

//                PLAY 1 - 0 "BIG_PHOT_BG1" SLIDE 1 Linear RIGHT   LOOP
                TellToCaspar("PLAY " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-0 \"" + bgfile + "\" SLIDE 1 Linear RIGHT LOOP");

                TellToCaspar(CGPrefixScoreBoard + " ADD 1 \"html/DidiScena/Z2\" 0");


                txtCmdBody.Text = "createItems(" + "'" + txtContestantCount.Text + "',";
                for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
                {

                    //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;

                    tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[4].Value + "'";
                    if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                        tmpStr = tmpStr + ",";

                }

                txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";
                TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");
                btnLoadScene.Text = "UnLoad ScoreBoard Scene";
            }
            else
            {
                TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);
                btnLoadScene.Text = "Load ScoreBoard Scene";
            }

            //txtCmdBody.Text = "Inv_AllIn(";


                //            txtCmdBody.Text = "Inv_updateNamesAnScoresX(";
                //           for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
                //            {
                //
                //               //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;
                //
                //               tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[2].Value + "'," + ContestantDataGrid.Rows[i].Cells[5].Value;
                //                if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                //                   tmpStr = tmpStr + ",";
                //
                //           }
                //            txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";
                //
                //           TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");


            }

        private void btnUnLoadAll_Click(object sender, EventArgs e)
        {
            // string CGPrefix = cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();


            //TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);
            //TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);

            TellToCaspar("CLEAR " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString());
            TellToCaspar("CLEAR " + cBox_CGCurrLowerTitleChannel.SelectedItem.ToString());


            //TellToCaspar("CLEAR " + CGPrefix);
            //TellToCaspar("CLEAR " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 1).ToString());

        }

        private void ContestantDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            lblCurrCont.Text = e.RowIndex.ToString() + " - " + e.ColumnIndex.ToString() + " " + ContestantDataGrid.Rows[e.RowIndex].Cells[0].Value;
            lblCurrContestant.ForeColor = Color.Green;
            lblCurrContestant.Text = ListContestant[e.RowIndex].Gvari;
            txtScoreToBeAdded.Text = "0";


            //button29.PerformClick();

            setAllScoresWhite();
            btn_title_InOut.Enabled = true;
            if (cBox_AutoTitleOnOFF.Checked)
            {
                btn_title_InOut.PerformClick();
            }

            btnVoteStart.BackColor = Color.LightGreen;


        }

        private void ContestantDataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {

            string strColumnName = ContestantDataGrid.Columns[e.ColumnIndex].Name;
            SortOrder strSortOrder = getSortOrder(e.ColumnIndex);


            if (strSortOrder == SortOrder.Ascending)
            {
                ListContestant = ListContestant.OrderBy(x => typeof(Contestant).GetProperty(strColumnName).GetValue(x, null)).ToList();
            }
            else
            {
                ListContestant = ListContestant.OrderByDescending(x => typeof(Contestant).GetProperty(strColumnName).GetValue(x, null)).ToList();
            }
            ContestantDataGrid.DataSource = ListContestant;
            ContestantDataGrid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = strSortOrder;

        }

        private void btnBonus8_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus9_Click(object sender, EventArgs e)
        {

        }

        private void btnBonus10_Click(object sender, EventArgs e)
        {

        }

        private void btnLoadEliminateConts_Click(object sender, EventArgs e)
        {
            string[] ElimMomgerali = new string[2];
            string[] ElimPhoneNum = new string[2];


            //ListContestant.Clear();
            //dataGridView1.Rows.Clear();

            //CGPrefixScoreBoard = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString(); ;
            //CGPrefixBonusBoard = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 1).ToString();
            CGPrefixEliminateBoard = "CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrScoreBoardChannelLayer.Text) + 2).ToString();
            //CGPrefixLowerThirdBoard = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 3).ToString();








            var fileStream = new FileStream(@"c:\CGData\Elimcontestants.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };
                UInt16 i = 0;
                while (((line = streamReader.ReadLine()) != null) && (i < Convert.ToUInt16(txtContestantCount.Text)))
                {
                    lineSplited = line.Split(',');
                    ElimPhoneNum[i] = lineSplited[0];
                    ElimMomgerali[i] = lineSplited[1];
                    i++;

                    //var PhoneCallObj = new PhoneAndPhoneCalls();
                    //PhoneCallObj.Phone = lineSplited[0];
                    //PhoneCallObj.PhoneCalls = Convert.ToUInt32(lineSplited[1]);
                    //ListPhoneCalls.Add(PhoneCallObj);

                }


            }


            txtElimContName0.Text = ElimMomgerali[0];
            txtElimContPhoneNum0.Text = ElimPhoneNum[0];
            txtElimContScore0.Text = "0";


            txtElimContName1.Text = ElimMomgerali[1];
            txtElimContPhoneNum1.Text = ElimPhoneNum[1];
            txtElimContScore1.Text = "0";


        }

        private void btnEliminateCallsImport_Click(object sender, EventArgs e)
        {
            var fileStream = new FileStream(@"c:\CGData\data.txt", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                string[] lineSplited = { "", "" };

                //while (((line = streamReader.ReadLine()) != null) && (i < Convert.ToUInt16(txtContestantCount.Text)))
                while ((line = streamReader.ReadLine()) != null)
                {
                    lineSplited = line.Split(',');
                    var PhoneCallObj = new PhoneAndPhoneCalls();                    
                    PhoneCallObj.Phone = lineSplited[0];
                    PhoneCallObj.PhoneCalls = Convert.ToUInt32(lineSplited[1]);

                    if (PhoneCallObj.Phone == txtElimContPhoneNum0.Text)
                        txtElimContScore0.Text = PhoneCallObj.PhoneCalls.ToString();

                    if (PhoneCallObj.Phone == txtElimContPhoneNum1.Text)
                        txtElimContScore1.Text = PhoneCallObj.PhoneCalls.ToString();



                    //ListPhoneCalls.Add(PhoneCallObj);

                }

            }

            

        }

        private void btnLoadEliminateScene_Click(object sender, EventArgs e)
        {
            TellToCaspar(CGPrefixEliminateBoard + " ADD 1 \"html/DidiScena/eliminate\" 0");


            txtCmdBody.Text = "createItems(" + "'" + "2" + "','" + txtElimContName0.Text + "','" + txtElimContName1.Text + "')";
            TellToCaspar(CGPrefixEliminateBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");



            


        }

        private void btnEliminateAllIn_Click(object sender, EventArgs e)
        {
            txtCmdBody.Text = "allIn('contItem00','contItem01')";
            TellToCaspar(CGPrefixEliminateBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");
        }

        private void btnEliminateGo_Click(object sender, EventArgs e)
        {
            if (txtElimContScore0.Text == txtElimContScore1.Text)
                return;

            if (Convert.ToInt32(txtElimContScore0.Text) > Convert.ToInt32(txtElimContScore1.Text))
                txtCmdBody.Text = "reUpdateAndEliminate('_ScoreCont00'," + txtElimContScore0.Text + ", '#contItem00', 1, 'down', '#contItem01')";
            else
                txtCmdBody.Text = "reUpdateAndEliminate('_ScoreCont01'," + txtElimContScore1.Text + ", '#contItem01', 1, 'up', '#contItem00')";


            TellToCaspar(CGPrefixEliminateBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");
        }


        private void UpdateControls(bool listening)
        {
            buttonStartListen.Enabled = !listening;
            buttonStopListen.Enabled = listening;
        }
        private void buttonStartListen_Click(object sender, EventArgs e)
        {
            

            try
            {
                // Check the port value
                if (txt_Port.Text == "")
                {
                    MessageBox.Show("Please enter a Port Number");
                    return;
                }
                string portStr = txt_Port.Text;
                int port = System.Convert.ToInt32(portStr);
                // Create the listening socket...
                m_mainSocket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream,
                                          ProtocolType.Tcp);
                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, port);
                // Bind to local IP Address...
                m_mainSocket.Bind(ipLocal);
                // Start listening...
                m_mainSocket.Listen(4);
                // Create the call back for any client connections...
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);

                UpdateControls(true);

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }




        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                // Here we complete/end the BeginAccept() asynchronous call
                // by calling EndAccept() - which returns the reference to
                // a new Socket object
                m_workerSocket[m_clientCount] = m_mainSocket.EndAccept(asyn);

                string clientIPAddress = (m_workerSocket[m_clientCount].RemoteEndPoint.ToString()).Split(':')[0];
                string clientProcessId = (m_workerSocket[m_clientCount].RemoteEndPoint.ToString()).Split(':')[1];

                // Let the worker Socket do the further processing for the 
                // just connected client
                WaitForData(m_workerSocket[m_clientCount]);
                // Now increment the client count
                ++m_clientCount;
                // Display this client connection as a status message on the GUI	
                String str = String.Format("Client # {0} connected", m_clientCount);
                if (tabPage7.InvokeRequired)
                {
                 
                    tabPage7.Invoke((Action)delegate { tabPage7.Text = "VOTTING Server:" + str; });

                }

                if (richTextBoxConnectedClientsMsg.InvokeRequired)
                {
//                    textBoxMsg.Invoke((Action)delegate { textBoxMsg.Text = str; });

                    richTextBoxConnectedClientsMsg.Invoke(new MethodInvoker(delegate { richTextBoxConnectedClientsMsg.AppendText(" \n\nConnected Client From " + clientIPAddress); }));
                    richTextBoxConnectedClientsMsg.Invoke(new MethodInvoker(delegate { richTextBoxConnectedClientsMsg.AppendText(" \nConnected Client ID  " + clientProcessId); }));
                    richTextBoxConnectedClientsMsg.Invoke(new MethodInvoker(delegate { richTextBoxConnectedClientsMsg.AppendText(" \n" + str); }));

                    if (clientIPAddress == (grBoxJiuri0.Text).Split(':')[1])
                    {
                        grBoxJiuri0.ForeColor = Color.Green;
                    }
                    else if (clientIPAddress == (grBoxJiuri1.Text).Split(':')[1])
                    {
                        grBoxJiuri1.ForeColor = Color.Green;
                    }
                    else if (clientIPAddress == (grBoxJiuri2.Text).Split(':')[1])
                    {
                        grBoxJiuri2.ForeColor = Color.Green;
                    }
                    else if (clientIPAddress == (grBoxJiuri3.Text).Split(':')[1])
                    {
                        grBoxJiuri3.ForeColor = Color.Green;
                    }
                    else if (clientIPAddress == (grBoxJiuri4.Text).Split(':')[1])
                    {
                        grBoxJiuri4.ForeColor = Color.Green;
                    }


                }
                else
                {
                    richTextBoxConnectedClientsMsg.AppendText(str);
                }

                //textBoxMsg.Invoke(textBoxMsg.Text=str);



                // Since the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }

        }

        public class SocketPacket
        {
            public System.Net.Sockets.Socket m_currentSocket;
            public byte[] dataBuffer = new byte[2];
        }

        public void WaitForData(System.Net.Sockets.Socket soc)
        {
            try
            {
                if (pfnWorkerCallBack == null)
                {
                    // Specify the call back function which is to be 
                    // invoked when there is any write activity by the 
                    // connected client
                    pfnWorkerCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.m_currentSocket = soc;
                // Start receiving any data written by the connected client
                // asynchronously
                soc.BeginReceive(theSocPkt.dataBuffer, 0,
                                   theSocPkt.dataBuffer.Length,
                                   SocketFlags.None,
                                   pfnWorkerCallBack,
                                   theSocPkt);
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }

        }


        public void OnDataReceived(IAsyncResult asyn)
        {

            String CurrJudgeIP = "";
            try
            {
                SocketPacket socketData = (SocketPacket)asyn.AsyncState;
                CurrJudgeIP = socketData.m_currentSocket.RemoteEndPoint.ToString();

                int iRx = 0;
                // Complete the BeginReceive() asynchronous call by EndReceive() method
                // which will return the number of characters written to the stream 
                // by the client
                iRx = socketData.m_currentSocket.EndReceive(asyn);

                string clientIPAddress = (socketData.m_currentSocket.RemoteEndPoint.ToString()).Split(':')[0];

                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.dataBuffer,0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                if (richTextBoxReceivedMsg.InvokeRequired)
                {

                    richTextBoxReceivedMsg.Invoke(new MethodInvoker(delegate { richTextBoxReceivedMsg.AppendText(Environment.NewLine + clientIPAddress + " " + szData + Environment.NewLine); }));
                    //richTextBoxReceivedMsg.Invoke(new MethodInvoker(delegate { richTextBoxReceivedMsg.AppendText(Environment.NewLine + socketData.m_currentSocket.RemoteEndPoint + " " + szData + Environment.NewLine); }));                    richTextBoxReceivedMsg.Invoke(new MethodInvoker(delegate { richTextBoxReceivedMsg.AppendText(Environment.NewLine + socketData.m_currentSocket.RemoteEndPoint + " " + szData + Environment.NewLine); }));

                    //                    richTextBoxReceivedMsg.Invoke(new MethodInvoker(delegate { richTextBoxReceivedMsg.AppendText(szData); }));


                    if (clientIPAddress == (grBoxJiuri0.Text).Split(':')[1])
                    {
                        if (txtJudgeValue0.InvokeRequired)
                            txtJudgeValue0.Invoke((Action)delegate { txtJudgeValue0.Text = szData; });
                        
                    }
                    else if (clientIPAddress == (grBoxJiuri1.Text).Split(':')[1])
                    {
                        if (txtJudgeValue1.InvokeRequired)
                            txtJudgeValue1.Invoke((Action)delegate { txtJudgeValue1.Text = szData; });

                    }
                    else if (clientIPAddress == (grBoxJiuri2.Text).Split(':')[1])
                    {
                        if (txtJudgeValue2.InvokeRequired)
                            txtJudgeValue2.Invoke((Action)delegate { txtJudgeValue2.Text = szData; });

                    }
                    else if (clientIPAddress == (grBoxJiuri3.Text).Split(':')[1])
                    {
                        if (txtJudgeValue3.InvokeRequired)
                            txtJudgeValue3.Invoke((Action)delegate { txtJudgeValue3.Text = szData; });

                    }
                    else if (clientIPAddress == (grBoxJiuri4.Text).Split(':')[1])
                    {
                        if (txtJudgeValue4.InvokeRequired)
                            txtJudgeValue4.Invoke((Action)delegate { txtJudgeValue4.Text = szData; });

                    }


                }
                else
                {
                    richTextBoxReceivedMsg.AppendText(szData);
                }

                // Continue the waiting for data on the Socket
                WaitForData(socketData.m_currentSocket);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show("უკაცრავად, კავშირი დაიკარგა ჟიურისთან, \n" + CurrJudgeIP + "-დან\n"+ se.Message);


            }
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            try
            {
                Object objData = richTextBoxSendMsg.Text;
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(objData.ToString());
                for (int i = 0; i < m_clientCount; i++)
                {
                    if (m_workerSocket[i] != null)
                    {
                        if (m_workerSocket[i].Connected)
                        {
                            m_workerSocket[i].Send(byData);
                        }
                    }
                }

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void buttonStopListen_Click(object sender, EventArgs e)
        {
            CloseSockets();
            UpdateControls(false);
        }


        void CloseSockets()
        {
            if (m_mainSocket != null)
            {
                m_mainSocket.Close();
            }
            for (int i = 0; i < m_clientCount; i++)
            {
                if (m_workerSocket[i] != null)
                {
                    m_workerSocket[i].Close();
                    m_workerSocket[i] = null;
                }
            }
        }

        private void richTextBoxReceivedMsg_TextChanged(object sender, EventArgs e)
        {

        }

        private void btt_J_0_10_Click(object sender, EventArgs e)
        {

        }

        private void btnUpdateFromJudges_Click(object sender, EventArgs e)
        {

            if ((ContestantDataGrid.Rows.Count <= 1) || (lblCurrContestant.Text == "აარჩიე მომღერალი"))
            {
                return;
            }

            txtScoreToBeAdded.Text = (Convert.ToUInt16(txtJudgeValue0.Text) + Convert.ToUInt16(txtJudgeValue1.Text) + Convert.ToUInt16(txtJudgeValue2.Text) + Convert.ToUInt16(txtJudgeValue3.Text) + Convert.ToUInt16(txtJudgeValue4.Text) ).ToString();
            btnPLUS.Enabled = true;            
            btnPLUS.BackColor = Color.LightGreen;
            btnUpdateFromJudges.BackColor = SystemColors.ButtonFace;
        }

        private void btnVoteStart_Click(object sender, EventArgs e)
        {

            txtJudgeValuesToZero();

            if (cBox_JudgesScoreShowNoneFullFrame.Checked)
            {
                TellToCaspar(CGPrefixJudgesScoresBoard + " ADD 1 \"html/DidiScena/judges1\" 0");
            }


            if (cBox_JudgesScoreShowFullFrame.Checked)
            { 
                string bgfile = "BigSceneBg";                    
            TellToCaspar("PLAY " + cBox_CGJudgesScoreChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGJudgesScoreChannelLayer.Text) - 1).ToString() + " \"" + bgfile + "\" SLIDE 1 Linear RIGHT LOOP");
            TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " ADD 1 \"html/DidiScena/judgesFullFramehd\" 0");
            TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " INVOKE 0 \"" + "ContestantSlideIn('" + ContestantDataGrid.Rows[ContestantDataGrid.CurrentRow.Index].Cells[6].Value + "')" + "\"");
            }

            JudgeTurn = 0;

            ListVoterJudges[0].Voted = false;
            ListVoterJudges[1].Voted = false;
            ListVoterJudges[2].Voted = false;
            ListVoterJudges[3].Voted = false;

            btnVoteStart.BackColor = SystemColors.ButtonFace;
            


            try
            {
                richTextBoxSendMsg.Clear();
                richTextBoxSendMsg.AppendText("r");
                Object objData = richTextBoxSendMsg.Text;
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(objData.ToString());
                for (int i = 0; i < m_clientCount; i++)
                {
                    if (m_workerSocket[i] != null)
                    {
                        if (m_workerSocket[i].Connected)
                        {
                            m_workerSocket[i].Send(byData);
                        }
                    }
                }

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void ContestantDataGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("შეცდომა გრიდში " + e.ToString());
        }

        private void btnAddWinnerToList_Click(object sender, EventArgs e)
        {
            //var fileStream = new FileStream(@"c:\CGData\contestants.txt", FileMode.Open, FileAccess.Write);
            //using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))

            //{

            if (Convert.ToInt32(txtElimContScore0.Text) < Convert.ToInt32(txtElimContScore1.Text))
                    File.AppendAllText(@"c:\CGData\contestants.txt", Environment.NewLine + txtElimContPhoneNum1.Text + "," + txtElimContName1.Text + Environment.NewLine);

                //streamWriter.WriteLine(txtElimContPhoneNum1.Text + "," + txtElimContName1.Text,true);                    
                else
                File.AppendAllText(@"c:\CGData\contestants.txt", Environment.NewLine + txtElimContPhoneNum0.Text + "," + txtElimContName0.Text + Environment.NewLine);
            //streamWriter.WriteLine(txtElimContPhoneNum0.Text + "," + txtElimContName0.Text,true);

            //}

        }

        private void ContestantDataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtJudgeValue0_TextChanged(object sender, EventArgs e)
        {
         //   if 
            

        }

        private void txtJudgeValues_TextChanged(object sender, EventArgs e)
        {


            if ((JudgeTurn < 5) && cBox_JudgesScoreShowFullFrame.Checked)
            {

                if (((sender as TextBox).Name.EndsWith("0")) && (!ListVoterJudges[0].Voted))
                {
                    if (Convert.ToInt16((sender as TextBox).Text) > 0)
                    {
                        JudgeTurn++;
                        if (cBox_JudgesScoreShowNoneFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoard + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[0].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        if (cBox_JudgesScoreShowFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[0].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        ListVoterJudges[0].Voted = true;
                    }
                }
                else if (((sender as TextBox).Name.EndsWith("1")) && (!ListVoterJudges[1].Voted))
                {
                    if (Convert.ToInt16((sender as TextBox).Text) > 0)
                    {
                        JudgeTurn++;
                        if (cBox_JudgesScoreShowNoneFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoard + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[1].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        if (cBox_JudgesScoreShowFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[1].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        ListVoterJudges[1].Voted = true;
                    }
                }
                else if (((sender as TextBox).Name.EndsWith("2")) && (!ListVoterJudges[2].Voted))
                {
                    if (Convert.ToInt16((sender as TextBox).Text) > 0)
                    {
                        JudgeTurn++;
                        if (cBox_JudgesScoreShowNoneFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoard + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[2].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        if (cBox_JudgesScoreShowFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[2].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        ListVoterJudges[2].Voted = true;
                    }
                }
                else if (((sender as TextBox).Name.EndsWith("3")) && (!ListVoterJudges[3].Voted))
                {
                    if (Convert.ToInt16((sender as TextBox).Text) > 0)
                    {
                        JudgeTurn++;
                        if (cBox_JudgesScoreShowNoneFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoard + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[3].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        if (cBox_JudgesScoreShowFullFrame.Checked) TellToCaspar(CGPrefixJudgesScoresBoardFullFrame + " INVOKE 0 \"" + "JudgeScoreSlideIn(" + (JudgeTurn - 1).ToString() + ",'" + ListVoterJudges[3].JudgeName + "','" + (sender as TextBox).Text + "')" + "\"");
                        ListVoterJudges[3].Voted = true;
                    }
                }
            }



            if ((Convert.ToInt16(txtJudgeValue0.Text) > 0) && (Convert.ToInt16(txtJudgeValue1.Text) > 0) && (Convert.ToInt16(txtJudgeValue2.Text) > 0) && (Convert.ToInt16(txtJudgeValue3.Text) > 0) && (Convert.ToInt16(txtJudgeValue4.Text) > 0))                
                btnUpdateFromJudges.BackColor = Color.LightGreen;
            else
                btnUpdateFromJudges.BackColor = SystemColors.ButtonFace;



        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void dataGridPhoneCalls_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //Int32 CurrRowIndex = Convert.ToInt16(((sender as Button).Name).Split('_')[1]);
            //Int32 CurrRowIndexInGrid = -1;

            dataGridPhoneCalls.ClearSelection();
            dataGridPhoneCalls.Rows[0].Selected = true;

            //txtScoreToBeAdded.Text = (sender as Button).Text;// "50";
            //int currRow = -1;

            string searchValue = ""; //dataGridPhoneCalls.Rows[CurrRowIndex].Cells[0].Value.ToString();
            int phoneCallValue = 0;// Convert.ToInt32(dataGridPhoneCalls.Rows[CurrRowIndex].Cells[1].Value.ToString());

            ContestantDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            ContestantDataGrid.ClearSelection();


            try
            {
                foreach (DataGridViewRow rowPhone in dataGridPhoneCalls.Rows)
                {
                    searchValue = rowPhone.Cells[0].Value.ToString();
                    phoneCallValue = Convert.ToInt32(rowPhone.Cells[1].Value.ToString());

                    foreach (DataGridViewRow row in ContestantDataGrid.Rows)
                    {
                        if (row.Cells[6].Value.ToString().Equals(searchValue))
                        {
                            row.Selected = true;
                            //CurrRowIndexInGrid = row.Index;
                            //row.Cells[7].Value = phoneCallValue;

                            for (UInt16 i = 0; i < ListContestant.Count; i++)
                            {
                                if (ListContestant[i].Phone == searchValue)
                                {
                                    ListContestant[i].PhoneCalls = phoneCallValue;
                                    //ListContestant[i].Position = -999;
                                    break;
                                }
                            }


                            //ContestantDataGrid.Rows[CurrRowIndexInGrid].Cells[0].Value = phoneCallValue;
                            break;
                        }
                    }

                }

  
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
                //CurrRowIndex = -1;
            }
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel3_DoubleClick(object sender, EventArgs e)
        {
            //ContestantDataGrid.Sort(ContestantDataGrid.Columns["Score"], ListSortDirection.Ascending);
            //ContestantDataGrid.Sort(ContestantDataGrid.Columns["PhoneCalls"], ListSortDirection.Ascending);

            ContestantSource.Sort = "Score, PhoneCalls";
        }

        private void SortDataByMultiColumns()
        {
           
            
        }

        private void button29_Click_2(object sender, EventArgs e)
        {
            Random r = new Random();

            // If the line doesn't contain the word 'Second', write the line to the file.



            //txtJudgeValue0.Text = (r.Next(1,10)).ToString();
            //txtJudgeValue1.Text = (r.Next(1, 10)).ToString();
            //txtJudgeValue2.Text = (r.Next(1, 10)).ToString();
            //txtJudgeValue3.Text = (r.Next(1, 10)).ToString();
            txtJudgeValue4.Text = (r.Next(1, 10)).ToString();

            

        }

        private void logConsole1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnResetCalls_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in ContestantDataGrid.Rows)
            {
                row.Cells[7].Value = 0 ;
            }

        }

        private void button30_Click_2(object sender, EventArgs e)
        {
            ListContestant.Clear();
            //ListContestant.RemoveAll();
            ListVoterJudges.Clear();
            ContestantSource.Clear();
            //ContestantDataGrid.Rows.Clear();
        }

        private void btnALLShow_Click(object sender, EventArgs e)
        {
            //TellToCaspar("CLEAR " + cBox_CGCurrChannel.SelectedItem.ToString());

            ClearLayers();

            TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);


            string tmpStr = "";
            //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();

            TellToCaspar(CGPrefixScoreBoard + " ADD 1 \"html/DidiScena/Z2\" 0");


            txtCmdBody.Text = "createItemsNext(" ;
            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {

                //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;

                tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[0].Value + "',";
                tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[4].Value + "',";
                tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[5].Value + "'";
                //tmpStr = tmpStr + "'" + ListContestantInStart[i].Gvari + "'";
                if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                    tmpStr = tmpStr + ",";

            }

            txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";




            TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");





            txtCmdBody.Text = "allIn(";



            tmpStr = "";
            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;
                tmpStr = tmpStr + "'" + ListContestantInStart[i].ContItemID + "'";//"'," + dataGridView1.Rows[i].Cells[5].Value;
                if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                    tmpStr = tmpStr + ",";

            }





            txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";
            TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");




        }

        private void btnSendCommand_Click_1(object sender, EventArgs e)
        {

        }

        private void btnGO_2_CG_Click(object sender, EventArgs e)
        {
            if (cBoxAMPCcmd.SelectedItem.ToString() == "INVOKE")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"" + txtCmdBody.Text + "\"");
            else if (cBoxAMPCcmd.SelectedItem.ToString() == "ADD & PLAY")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ADD " + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " \"html/DidiScena/" + txtCmdBody.Text + "\" 1");
            else if (cBoxAMPCcmd.SelectedItem.ToString() == "ADD")
                TellToCaspar("CG " + cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " ADD " + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString() + " \"html/DidiScena/" + txtCmdBody.Text + "\" 0");


        }

        private void txtCmdBody_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtCmdBody_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void txtCmdBody_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                btnGO_2_CG.PerformClick();
            }
        }

        private void btn_title_In_Click(object sender, EventArgs e)
        {


        }

        private void btn_title_Out_Click(object sender, EventArgs e)
        {
 



            
        }

        private void button33_Click(object sender, EventArgs e)
        {
            TellToCaspar("CLEAR " + ChannelAndLayerOfEliminateBoard);
        }




        private void ClearLayers()
        { }

        private void btnVoteStandBy_Click(object sender, EventArgs e)
        {


/*
            ChannelAndLayerOfScoreBoard = cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfBonusBoard = cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 1).ToString();
            ChannelAndLayerOfEliminateBoard = cBox_CGCurrChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrChannelLayer.Text) + 2).ToString();
            ChannelAndLayerOfLowerThirdBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + cBox_CGCurrLowerTitleChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfJudgesScoresBoardFullFrame = cBox_CGJudgesScoreChannel.SelectedItem.ToString() + "-" + cBox_CGJudgesScoreChannelLayer.SelectedItem.ToString();
            ChannelAndLayerOfJudgesScoresBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGCurrLowerTitleChannelLayer.Text) + 2).ToString();
*/

            String tmpstring = cBox_CGJudgesScoreChannel.SelectedItem.ToString() + "-" + (Convert.ToInt16(cBox_CGJudgesScoreChannelLayer.Text) - 1).ToString();
        
            TellToCaspar(CGPrefixJudgesScoresBoard + " INVOKE 0 \"" + "goOutJudgeBoxes()" + "\"");

            TellToCaspar("CLEAR " + ChannelAndLayerOfJudgesScoresBoard);

            //TellToCaspar("CLEAR " + tmpstring);
            TellToCaspar("CLEAR " + ChannelAndLayerOfJudgesScoresBoardFullFrame);

            JudgeTurn = 0;


            ListVoterJudges[0].Voted = false;
            ListVoterJudges[1].Voted = false;
            ListVoterJudges[2].Voted = false;
            ListVoterJudges[3].Voted = false;


            try
            {
                richTextBoxSendMsg.Clear();
                richTextBoxSendMsg.AppendText("n");
                Object objData = richTextBoxSendMsg.Text;
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(objData.ToString());
                for (int i = 0; i < m_clientCount; i++)
                {
                    if (m_workerSocket[i] != null)
                    {
                        if (m_workerSocket[i].Connected)
                        {
                            m_workerSocket[i].Send(byData);
                        }
                    }
                }

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }





        private void setAllScoresWhite()
        {
            string tmpStr = "";
            //string CGPrefix = "CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString();
            //txtCmdBody.Text = "Inv_AllIn(";
            txtCmdBody.Text = "colorToWhite(";



            ChannelAndLayerOfScoreBoard = cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrScoreBoardChannelLayer.SelectedItem.ToString();
            CGPrefixScoreBoard = "CG " + ChannelAndLayerOfScoreBoard;





            for (UInt16 i = 0; i < Convert.ToUInt16(txtContestantCount.Text); i++)
            {
                //tmpStr = tmpStr + "'" + ListContestant[i].Gvari + "'," + ListContestant[i].Score;
                tmpStr = tmpStr + "'" + ContestantDataGrid.Rows[i].Cells[2].Value + "'";//"'," + dataGridView1.Rows[i].Cells[5].Value;
                if (i != (Convert.ToUInt16(txtContestantCount.Text) - 1))
                    tmpStr = tmpStr + ",";

            }





            txtCmdBody.Text = txtCmdBody.Text + tmpStr + ")";
            TellToCaspar(CGPrefixScoreBoard + " INVOKE 0 \"" + txtCmdBody.Text + "\"");


            //TellToCaspar("CG " + cBox_CGCurrChannel.SelectedItem.ToString() + "-" + cBox_CGCurrChannelLayer.SelectedItem.ToString() + " INVOKE 0 \"Inv_AllIn\"");


            //            txtCmdBody.Text = "reArrangeItemsFromTo1('" + ListContestant[currContestantPos].ScoreID + "'," + currContestantNewScore.ToString() +
            //                ",'#" + ListContestant[currContestantPos].ContItemID + "','" + UpPrefix + (currContestantDeltaPosition * nominalPosDelta).ToString() + "'" + ")";





        }

        private void btn_title_InOut_Click(object sender, EventArgs e)
        {
            if ((ContestantDataGrid.Rows.Count <= 1) || (lblCurrContestant.Text == "აარჩიე მომღერალი"))
            {
                return;
            }
            else
            {

                TitleInOut();

            }
        }



        public void  TitleInOut()
        {


            ChannelAndLayerOfLowerThirdBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + cBox_CGCurrLowerTitleChannelLayer.SelectedItem.ToString();
            CGPrefixLowerThirdBoard = "CG " + ChannelAndLayerOfLowerThirdBoard;

            if (cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() == cBox_CGCurrLowerTitleChannel.SelectedItem.ToString())
                TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);



            if (btn_title_InOut.Text == "TITLE IN")
            {


                //lblCurrCont.Text = e.RowIndex.ToString() + " - " + e.ColumnIndex.ToString() + " " + ContestantDataGrid.Rows[e.RowIndex].Cells[0].Value;


                TellToCaspar(CGPrefixLowerThirdBoard + " ADD 1 \"html/DidiScena/lowertitle\" 0");
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "createTitleBoxItems(" + ContestantDataGrid.Rows[ContestantDataGrid.CurrentRow.Index].Cells[6].Value + ")" + "\"");
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "go1()" + "\"");

                btn_title_InOut.Text = "TITLE OUT";
            }
            else
            {
                //TellToCaspar("CLEAR " + ChannelAndLayerOfLowerThirdBoard);
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "TitleBoxGoAway(1)" + "\"");
                btn_title_InOut.Text = "TITLE IN";

            }
        }


        private void btn_Looptitle_InOut_Click(object sender, EventArgs e)
        {
            //CGPrefixLowerThirdBoard = "CG " + ;
            ChannelAndLayerOfLowerThirdBoard = cBox_CGCurrLowerTitleChannel.SelectedItem.ToString() + "-" + cBox_CGCurrLowerTitleChannelLayer.SelectedItem.ToString();
            CGPrefixLowerThirdBoard = "CG " + ChannelAndLayerOfLowerThirdBoard;

            if (cBox_CGScoreBoardCurrChannel.SelectedItem.ToString() == cBox_CGCurrLowerTitleChannel.SelectedItem.ToString())
                TellToCaspar("CLEAR " + ChannelAndLayerOfScoreBoard);



            if (btn_Looptitle_InOut.Text == "LOOP TITLE IN")
            {


                //lblCurrCont.Text = e.RowIndex.ToString() + " - " + e.ColumnIndex.ToString() + " " + ContestantDataGrid.Rows[e.RowIndex].Cells[0].Value;


                TellToCaspar(CGPrefixLowerThirdBoard + " ADD 1 \"html/DidiScena/lowertitle\" 0");
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "createTitleBoxItems(" + "MZA-ALL" + ")" + "\"");
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "go1()" + "\"");

                btn_Looptitle_InOut.Text = "LOOP TITLE OUT";
            }
            else
            {
                //TellToCaspar("CLEAR " + ChannelAndLayerOfLowerThirdBoard);
                TellToCaspar(CGPrefixLowerThirdBoard + " INVOKE 0 \"" + "TitleBoxGoAway(1)" + "\"");
                btn_Looptitle_InOut.Text = "LOOP TITLE IN";

            }
        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button29_Click_3(object sender, EventArgs e)
        {
            setAllScoresWhite();
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            grBoxJiuri0.Text = ListVoterJudges[0].JudgeName + ":" + txt_IPJiuri0.Text;
            

        }

        private void button31_Click(object sender, EventArgs e)
        {
            grBoxJiuri1.Text = ListVoterJudges[1].JudgeName + ":" + txt_IPJiuri1.Text;
        }

        private void button32_Click_1(object sender, EventArgs e)
        {
            grBoxJiuri2.Text = ListVoterJudges[2].JudgeName + ":" + txt_IPJiuri2.Text;
        }

        private void button34_Click_1(object sender, EventArgs e)
        {
            grBoxJiuri3.Text = ListVoterJudges[3].JudgeName + ":" + txt_IPJiuri3.Text;
        }
    }



    //private String TellToCaspar(String CGCmd)
}




