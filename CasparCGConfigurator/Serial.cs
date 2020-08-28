using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace CasparCGConfigurator
{
    
    class Serial
    {
        public static string[] ports;

        public static void UpdatePorts(MainForm form = null)
        //public static void UpdatePorts()
        {
            //string[] ports;
          

            ports = SerialPort.GetPortNames();

            //form.btnConnectOrDisConnect.Text = "dutddudd";

            form.cmbPortName.Items.Clear();
            foreach (string port in ports)
            {
                form.cmbPortName.Items.Add(port);
                
            }
        }


        public static bool ConnectSerialPort(MainForm form = null)
        {
            bool err = true;


            if (ConnectOnePortInForm(form.cmbPortName.SelectedIndex, form.cmbBoudRate.Text, form.cmbParity.Text, form.cmbDataBits.Text, form.cmbStopBits.Text, form))
            {
                
                //btnSend.Enabled = true;

                if (!form.rbHex.Checked & !form.rbText.Checked)
                {
                    form.rbText.Checked = true;
                }

                
                err = true;
            }
            else
            { 
                err = false;
            }

            return err;
        }

        public static bool DisconnectPort(MainForm form = null)
        {
            bool err = true;

            try
            {
                form.serialPort1.Close();

            }
            catch (UnauthorizedAccessException) { err = false; }
            catch (System.IO.IOException) { err = false; }
            catch (ArgumentException) { err = false; }

            if (err)
            {

            }

            return err;

        }

        //public static bool ConnectOnePort(SerialPort serialPort1, Int32 portNum, string portBoudRate, string portParity, string dataBits, string stopBits)
        public static bool ConnectOnePortInForm(Int32 portNum, string portBoudRate, string portParity, string dataBits, string stopBits, MainForm form = null)
        {
            bool err = true;



            try
            {
                form.serialPort1.Close();

                form.serialPort1.PortName = ports[portNum];
                form.serialPort1.BaudRate = int.Parse(portBoudRate);
                form.serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), portParity);
                form.serialPort1.DataBits = int.Parse(dataBits);
                form.serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);

                form.serialPort1.Open();
            }
            catch (UnauthorizedAccessException) { err = false; }
            catch (System.IO.IOException) { err = false; }
            catch (ArgumentException) { err = false; }


            return err;
        }

        public static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e, MainForm form = null)
        {
            //string receivedData = serialPort1.ReadExisting();
            string receivedData = form.serialPort1.ReadLine();
            Int16 playerIndex = -1;
            Int16 CurrNote = 0;

            

            if (receivedData.IndexOf("GET /ax_SW&sw=42") > -1)
            {

            }
        }


    }


    
    }
