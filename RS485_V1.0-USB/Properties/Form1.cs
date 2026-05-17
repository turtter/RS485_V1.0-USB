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
            groupBox3.Enabled = false; //tắt groupbox3
            groupBox4.Enabled = false; //tắt groupbox4
            groupBox5.Enabled = false; //tắt groupbox5
            groupBox6.Enabled = false; //tắt groupbox6
            groupBox7.Enabled = false; //tắt groupbox7
            groupBox8.Enabled = false; //tắt groupbox8
            groupBox9.Enabled = false; //tắt groupbox9
            comboBox3.Enabled = false; //tắt comboBox3  

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
                comboBox1.Items.Add(port);

            comboBox2.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" }); //thêm baud vào comboBox2
            comboBox2.SelectedItem = "9600";   //mặc định chọn 9600
            comboBox3.Items.AddRange(new object[] { "4CH", "6CH" });
            comboBox3.SelectedIndex = -1;


        }
        private ushort CRC16(byte[] data) //    hàm tính CRC16 cho dữ liệu truyền đi
        {
            ushort crc = 0xFFFF; // Giá trị khởi tạo

            for (int pos = 0; pos < data.Length; pos++)
            {
                crc ^= (ushort)data[pos]; 

                for (int i = 8; i != 0; i--) 
                {
                    if ((crc & 0x0001) != 0) 
                    {
                        crc >>= 1;           
                        crc ^= 0xA001;       
                    }
                    else                     
                    {
                        crc >>= 1;          
                    }
                }
            }

            return crc;
        }
        private byte[] TaoFrameRead(byte channel) //tao frame READ
        {
            byte[] frame = new byte[] { 0x3A, 0x03, 0x01, channel };
            ushort crc = CRC16(frame);
            return new byte[] { 0x3A, 0x03, 0x01, channel, (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }

        // Frame ghi theo form: 3A 06 02 [channel] [LSB] CRC_L CRC_H]
        private byte[] TaoFrameWriteSingleChannel(byte channel, int value)
        {
            byte vLSB = (byte)(value & 0xFF);
            byte[] frame = new byte[] { 0x3A, 0x06, 0x02, channel, vLSB };
            ushort crc = CRC16(frame);
            return new byte[] { 0x3A, 0x06, 0x02, channel, vLSB,
                        (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }

        private byte[] TaoFrameWriteAllChannels(int[] values)
        {
            int numCH = values.Length;                             // 4 hoặc 6 kênh 

            // data = [MSB] [LSB] cho từng kênh, KHÔNG có byte channel
            int dataSize = numCH * 2;                              // 4CH=8 bytes, 6CH=12 bytes

            byte[] frame = new byte[3 + dataSize + 2];
            frame[0] = 0x3A;
            frame[1] = 0x06;
            frame[2] = (byte)dataSize; 

            for (int i = 0; i < numCH; i++)
            {
                frame[3 + i * 2] = (byte)(values[i] >> 8);          // MSB
                frame[3 + i * 2 + 1] = (byte)(values[i] & 0xFF);    // LSB
            }

            byte[] frameForCRC = new byte[3 + dataSize];
            Array.Copy(frame, frameForCRC, 3 + dataSize);
            ushort crc = CRC16(frameForCRC);

            frame[3 + dataSize] = (byte)(crc & 0xFF);
            frame[3 + dataSize + 1] = (byte)(crc >> 8);

            return frame;
        }

        private void ReadTimer_Tick(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;
            if (comboBox3.SelectedItem == null) return;

            if (comboBox3.SelectedItem.ToString() == "4CH")         // 4CH: Gửi lệnh đọc từng kênh một
            {
                for (byte ch = 1; ch <= 4; ch++)
                {
                    mySerialPort.Write(TaoFrameRead(ch), 0, 6);
                    System.Threading.Thread.Sleep(100);
                }
            }
            else // 6CH
            {
                mySerialPort.Write(TaoFrameRead(0x07), 0, 6);
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(100);

            if (!mySerialPort.IsOpen) return;

            int bytesAvailable = mySerialPort.BytesToRead;
            if (bytesAvailable == 0) return;

            byte[] buffer = new byte[bytesAvailable];
            mySerialPort.Read(buffer, 0, bytesAvailable);

            string hex = BitConverter.ToString(buffer).Replace("-", " ");

            if (this.IsDisposed) return;

            this.Invoke(new Action(() =>
            {
                textBox3.AppendText($"Nhận: {hex}{Environment.NewLine}");

                ParseMultipleFrames(buffer);
            }));
        }

        private void ParseMultipleFrames(byte[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                // Tìm header 0x3A
                if (data[i] != 0x3A)
                {
                    i++;
                    continue;
                }
                if (i + 3 >= data.Length) break;

                byte funcCode = data[i + 1];
                byte dataLen = data[i + 2];

                int frameLen = 3 + dataLen + 2;

                if (i + frameLen > data.Length) break;

                byte[] frame = new byte[frameLen];
                Array.Copy(data, i, frame, 0, frameLen);

                ParseResponse(frame);

                i += frameLen;
            }
        }

        private void ParseResponse(byte[] data)
        {
            if (data.Length < 6) return;
            if (data[0] != 0x3A) return;

            // Kiểm tra CRC
            byte[] dataWithoutCRC = new byte[data.Length - 2];
            Array.Copy(data, dataWithoutCRC, data.Length - 2);
            ushort calcCRC = CRC16(dataWithoutCRC);
            ushort recvCRC = (ushort)(data[data.Length - 2] | (data[data.Length - 1] << 8));

            if (calcCRC != recvCRC)
            {
                textBox3.AppendText($" CRC lỗi! Tính: {calcCRC:X4} - Nhận: {recvCRC:X4}{Environment.NewLine}");
                return;
            }

            byte funcCode = data[1];

            switch (funcCode)
            {
                case 0x03: // Response đọc value
                    byte channel = data[2];
                    int value = (data[3] << 8) | data[4];
                    // textBox3.AppendText($" Đọc CH{channel} = {value}{Environment.NewLine}");
                    break;

                case 0x06: // Response ghi từng kênh
                    textBox3.AppendText($" Ghi kênh thành công{Environment.NewLine}");
                    break;

                case 0x07: // Response ghi tất cả kênh
                    textBox3.AppendText($" Ghi tất cả kênh thành công{Environment.NewLine}");
                    break;

                case 0xA1: // Error
                    textBox3.AppendText($" Thiết bị báo lỗi: 0x{data[2]:X2}{Environment.NewLine}");
                    break;

                default:
                    textBox3.AppendText($" FuncCode 0x{funcCode:X2} - Data: {BitConverter.ToString(data).Replace("-", " ")}{Environment.NewLine}");
                    break;
            }
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

        private void DocKenh(byte ch)
        {
            if (!mySerialPort.IsOpen) return;
            mySerialPort.DiscardInBuffer();
            mySerialPort.Write(TaoFrameRead(ch), 0, 6);
            int elapsed = 0;
            while (mySerialPort.BytesToRead < 8 && elapsed < 1000) { System.Threading.Thread.Sleep(10); elapsed += 10; }
            if (mySerialPort.BytesToRead == 0) { textBox3.AppendText($"CH{ch}: No response\n"); return; }
            byte[] resp = new byte[mySerialPort.BytesToRead];
            mySerialPort.Read(resp, 0, resp.Length);
            textBox3.AppendText($"CH{ch}: {BitConverter.ToString(resp).Replace("-", " ")}\n");
        }

        private void GhiKenh(byte ch, int value)
        {
            if (!mySerialPort.IsOpen) return;
            byte[] frame = TaoFrameWriteSingleChannel(ch, value);
            mySerialPort.Write(frame, 0, frame.Length); // dùng frame.Length thay vì 8
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn cổng COM", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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
                textBox3.AppendText($" Đã kết nối {mySerialPort.PortName} - {mySerialPort.BaudRate} baud{Environment.NewLine}");
            }
            else
            {
                mySerialPort.Close();
                button1.Text = "CONNECT";
                groupBox9.Enabled = false;
                comboBox3.Enabled = false;
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
                groupBox4.Enabled = false;
                groupBox5.Enabled = false;
                groupBox6.Enabled = false;
                groupBox7.Enabled = false;
                groupBox8.Enabled = false;

                textBox3.AppendText($" Đã ngắt kết nối{Environment.NewLine}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;
            if (comboBox3.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn kênh 4CH hoặc 6CH!", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox3.SelectedItem.ToString() == "4CH")
            {
                // Đọc từng kênh 1→4
                for (byte ch = 1; ch <= 4; ch++)
                {
                    byte[] frame = TaoFrameRead(ch);
                    string log = BitConverter.ToString(frame).Replace("-", " ");
                    textBox3.AppendText($"Gửi đọc CH{ch}: {log}{Environment.NewLine}");
                    mySerialPort.Write(frame, 0, frame.Length);
                    System.Threading.Thread.Sleep(100);
                }
            }
            else // 6CH - Read All
            {
                byte[] frame = TaoFrameRead(0x07);
                string log = BitConverter.ToString(frame).Replace("-", " ");
                textBox3.AppendText($"Gửi đọc tất cả 6CH: {log}{Environment.NewLine}");
                mySerialPort.Write(frame, 0, frame.Length);
            }
         }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!mySerialPort.IsOpen) return;
            if (comboBox3.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn kênh 4CH hoặc 6CH!", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int value = (int)numericUpDown9.Value;
            byte vLSB = (byte)(value & 0xFF);

            if (comboBox3.SelectedItem.ToString() == "4CH")
            {
                // ==== XỬ LÝ 4CH: GHI TỪNG KÊNH MỘT ====
                for (byte ch = 1; ch <= 4; ch++)
                {
                    byte[] frame = new byte[] { 0x3A, 0x06, 0x02, ch, vLSB };
                    ushort crc = CRC16(frame);

                    byte[] fullFrame = new byte[] { 0x3A, 0x06, 0x02, ch, vLSB,
                                            (byte)(crc & 0xFF), (byte)(crc >> 8) };

                    string log = BitConverter.ToString(fullFrame).Replace("-", " ");
                    textBox3.AppendText($"Gửi ghi CH{ch} (value={value}): {log}{Environment.NewLine}");

                    mySerialPort.Write(fullFrame, 0, fullFrame.Length);

                    System.Threading.Thread.Sleep(100);
                }
            }
            else
            {
                // ==== XỬ LÝ 6CH: GHI TẤT CẢ CÙNG LÚC ====
                byte[] frame = new byte[] { 0x3A, 0x06, 0x02, 0x07, vLSB };
                ushort crc = CRC16(frame);

                byte[] fullFrame = new byte[] { 0x3A, 0x06, 0x02, 0x07, vLSB,
                                        (byte)(crc & 0xFF), (byte)(crc >> 8) };

                string log = BitConverter.ToString(fullFrame).Replace("-", " ");
                textBox3.AppendText($"Gửi ghi tất cả 6CH (value={value}): {log}{Environment.NewLine}");

                mySerialPort.Write(fullFrame, 0, fullFrame.Length);
            }
            }
        
        // ========== ĐÓNG APP ==========
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            readTimer.Stop();
            if (mySerialPort != null && mySerialPort.IsOpen)
                mySerialPort.Close();
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
            DocKenh(1);
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }
        private void label6_Click(object sender, EventArgs e)
        {
            GhiKenh(1, (int)numericUpDown1.Value);
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

        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {

        }

        private void groupBox9_Enter(object sender, EventArgs e)
        {

        }

        private void label29_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {
            GhiKenh(3, (int)numericUpDown3.Value);
        }

        private void label16_Click(object sender, EventArgs e)
        {
            DocKenh(3);
        }

        private void label20_Click(object sender, EventArgs e)
        {
            DocKenh(4);
        }

        private void label24_Click(object sender, EventArgs e)
        {
            DocKenh(5);
        }

        private void label28_Click(object sender, EventArgs e)
        {
            DocKenh(6);
        }

        private void label11_Click(object sender, EventArgs e)
        {
            GhiKenh(2, (int)numericUpDown2.Value);
        }

        private void label19_Click(object sender, EventArgs e)
        {
            GhiKenh(4, (int)numericUpDown4.Value);
        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {
            
        }

        private void label23_Click(object sender, EventArgs e)
        {
            GhiKenh(5, (int)numericUpDown2.Value);
        }

        private void label27_Click(object sender, EventArgs e)
        {
            GhiKenh(6, (int)numericUpDown2.Value);
        }

        private void label12_Click(object sender, EventArgs e)
        {
            DocKenh(2);
        }
    }
}
