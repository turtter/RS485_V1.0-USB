using System;
using System.Windows.Forms;
using System.IO.Ports;

namespace RS485_V1._0_USB
{
    public partial class Form1 : Form
    {
        private SerialPort mySerialPort = new SerialPort();
        private System.Windows.Forms.Timer readTimer = new System.Windows.Forms.Timer();
        public Form1()
        {
            InitializeComponent();
            mySerialPort.DataReceived += DataReceivedHandler;
            readTimer.Tick += ReadTimer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Enabled = true; //bật nút 1 kết nối 
            groupBox2.Enabled = false; //tắt groupbox2
            groupBox3.Enabled = false; //tắt groupbox3
            groupBox4.Enabled = false; //tắt groupbox4
            groupBox5.Enabled = false; //tắt groupbox5
            groupBox6.Enabled = false; //tắt groupbox6
            groupBox7.Enabled = false; //tắt groupbox7
            groupBox8.Enabled = false; //tắt groupbox8
            groupBox9.Enabled = false; //tắt groupbox9
            comboBox3.Enabled = false; //tắt comboBox3  

            string[] ports = SerialPort.GetPortNames(); //hàm đọc cổng COM có trên máy tính 19-21
            foreach (string port in ports)
                comboBox1.Items.Add(port);

            comboBox2.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" }); //thêm tốc độ baud vào comboBox2
            comboBox2.SelectedItem = "9600";   //mặc định chọn 9600
            comboBox3.Items.AddRange(new object[] { "4CH", "6CH" });
            comboBox3.SelectedIndex = -1;


        }
        private ushort CRC16(byte[] data) //    hàm tính CRC16 cho dữ liệu truyền đi
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }
        private byte[] TaoFrameRead(byte channel) //tao frame READ, frame có cấu trúc: [0x3A, 0x03, 0x01, channel, CRC_L, CRC_H]
        {
            byte[] frame = new byte[] { 0x3A, 0x03, 0x01, channel };
            ushort crc = CRC16(frame);
            return new byte[] { 0x3A, 0x03, 0x01, channel, (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }
        private byte[] TaoFrameWrite(byte channel, int value)
        {
            byte vMSB = (byte)(value >> 8);
            byte vLSB = (byte)(value & 0xFF);
            byte[] frame = new byte[] { 0x3A, 0x10, 0x06, channel, vMSB, vLSB };
            ushort crc = CRC16(frame);
            return new byte[] { 0x3A, 0x10, 0x06, channel, vMSB, vLSB, (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }
        private void ReadTimer_Tick(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;

            if (comboBox3.SelectedItem?.ToString() == "4CH")
                mySerialPort.Write(TaoFrameRead(0x05), 0, 6);
            else if (comboBox3.SelectedItem?.ToString() == "6CH")
                mySerialPort.Write(TaoFrameRead(0x07), 0, 6);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen)
            {
                mySerialPort.PortName = comboBox1.Text;
                mySerialPort.BaudRate = int.Parse(comboBox2.Text);
                mySerialPort.Parity = Parity.None;
                mySerialPort.DataBits = 8;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.Open();
                comboBox3.Enabled = true;
                button1.Text = "DISCONNECT";
                groupBox9.Enabled = true;
            }
            else
            {
                mySerialPort.Close();
                button1.Text = "CONNECT";
                groupBox9.Enabled = false;
                comboBox3.Enabled = false;
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(5000);

            if (!mySerialPort.IsOpen) return;

            string data = mySerialPort.ReadExisting();

            if (this.IsDisposed) return;

            this.Invoke(new Action(() =>
            {
                textBox3.AppendText(data);
            }));
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label31_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label33_Click(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem.ToString() == "4CH")
            {
                groupBox3.Enabled = true;
                groupBox4.Enabled = true;
                groupBox5.Enabled = true;
                groupBox6.Enabled = true;
                groupBox7.Enabled = false;
                groupBox8.Enabled = false;

            }
            else
            {
                groupBox3.Enabled = true;
                groupBox4.Enabled = true;
                groupBox5.Enabled = true;
                groupBox6.Enabled = true;
                groupBox7.Enabled = true;
                groupBox8.Enabled = true;

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;

            if (comboBox3.SelectedItem?.ToString() == "4CH")
                mySerialPort.Write(TaoFrameRead(0x05), 0, 6);
            else if (comboBox3.SelectedItem?.ToString() == "6CH")
                mySerialPort.Write(TaoFrameRead(0x07), 0, 6);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;

            int value = (int)numericUpDown9.Value;
            int channels = comboBox3.SelectedItem?.ToString() == "6CH" ? 6 : 4;

            for (byte ch = 1; ch <= channels; ch++)
            {
                byte[] frame = TaoFrameWrite(ch, value);
                string log = BitConverter.ToString(frame).Replace("-", " ");
                textBox3.AppendText("Gửi CH" + ch + ": " + log + Environment.NewLine);
                mySerialPort.Write(frame, 0, frame.Length);
                System.Threading.Thread.Sleep(50);
            }
        }
        // ========== ĐÓNG APP ==========
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            readTimer.Stop();
            if (mySerialPort != null && mySerialPort.IsOpen)
                mySerialPort.Close();
        }

        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
