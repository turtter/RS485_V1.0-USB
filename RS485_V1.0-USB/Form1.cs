using System;
using System.Windows.Forms;
using System.IO.Ports;
namespace RS485_V1._0_USB
{
    public partial class Form1 : Form
    {
        private SerialPort mySerialPort = new SerialPort();
        public Form1()
        {
            InitializeComponent();
            mySerialPort.DataReceived += DataReceivedHandler;
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

                button1.Text = "DISCONNECT";
                groupBox9.Enabled = true;
            }
            else
            {
                mySerialPort.Close();
                button1.Text = "CONNECT";
                groupBox9.Enabled = false;
            }
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(1000);

            if (!mySerialPort.IsOpen) return; 

            int bytes = mySerialPort.BytesToRead;
            byte[] buffer = new byte[bytes];
            mySerialPort.Read(buffer, 0, bytes);

            string hex = BitConverter.ToString(buffer).Replace("-", " ");

            this.Invoke(new Action(() =>
            {
                textBox3.AppendText(hex + Environment.NewLine);
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
            if (comboBox3.SelectedItem.ToString() ==  "4CH")
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
    }
}
