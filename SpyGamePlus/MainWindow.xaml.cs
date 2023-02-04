using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using SpyGamePlus_Client.Classes;

namespace SpyGamePlus
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int port = 8080;
        public static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public MainWindow()
        {
            InitializeComponent();

            IPAddress ipAddress = IPAddress.Parse("26.190.29.233");
            IPEndPoint Ep = new IPEndPoint(ipAddress, port);
            socket.Connect(Ep);
        }

        private void Sign_btn_Click(object sender, EventArgs e)
        {
            Sign_Name_txtbox.Visibility = Visibility.Visible;
            Sign_Pass_txtbox.Visibility = Visibility.Visible;
            Reg_btn.Visibility = Visibility.Hidden;
            Sign_btn.Visibility = Visibility.Hidden;
            State_lbl.Visibility = Visibility.Visible;
            PassName_Sign_btn.Visibility = Visibility.Visible;
            Come_Back_btn.Visibility = Visibility.Visible;
        }

        private void Reg_btn_Click(object sender, EventArgs e)
        {
            Reg_Name_txtbox.Visibility = Visibility.Visible;
            Reg_Pass_txtbox.Visibility = Visibility.Visible;
            Reg_RePass_txtbox.Visibility = Visibility.Visible;
            State_lbl.Visibility = Visibility.Visible;
            PassName_Reg_btn.Visibility = Visibility.Visible;
            Reg_btn.Visibility = Visibility.Hidden;
            Sign_btn.Visibility = Visibility.Hidden;
            Come_Back_btn.Visibility = Visibility.Visible;
        }

        private void PassName_Sign_btn_Click(object sender, EventArgs e)
        {
            NetLib.SendDataToNet(socket, "Sig/" + Sign_Name_txtbox.Text.Replace('/', ' ') + "/" + Sign_Pass_txtbox.Text.Replace('/', ' '));
            byte[] Data = new byte[64];
            socket.BeginReceive(Data, 0, Data.Length, 0, SigAnswerReceiverCallback, Data);
        }

        private void PassName_Reg_btn_Click(object sender, EventArgs e)
        {
            if (Reg_Pass_txtbox.Text == Reg_RePass_txtbox.Text)
            {
                NetLib.SendDataToNet(socket, "Reg/" + Reg_Name_txtbox.Text.Replace('/', ' ') + "/" + Reg_Pass_txtbox.Text.Replace('/', ' '));
                byte[] Data = new byte[64];
                socket.BeginReceive(Data, 0, Data.Length, 0, RegAnswerReceiverCallback, Data);
            }
            else
            {
                State_lbl.Text = "Пароли не совпадают. Попробуйте еще раз.";
                return;
            }
        }


        private void All_txtbox_Click(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Введите Имя" || textBox.Text == "Введите Пароль" || textBox.Text == "Повторите Пароль")
            {
                textBox.Text = "";
            }
        }

        private void RegAnswerReceiverCallback(IAsyncResult res)
        {
            string Ans = Encoding.Unicode.GetString(res.AsyncState as byte[], 0, socket.EndReceive(res));
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (Ans == "OK")
                {
                    State_lbl.Text = "OK";

                    string Name = Reg_Name_txtbox.Text;
                    Hide();
                    MenuWindow menuWindow = new MenuWindow(socket, Name, int.Parse(NetLib.ReadDataFromNet(socket)));
                    menuWindow.ShowDialog();
                    Close();
                }
                else if (Ans == "NameError")
                {
                    State_lbl.Text = "Такое имя уже существует. Попробуйте выбрать другое имя.";
                }
            }));
        } 

        private void SigAnswerReceiverCallback(IAsyncResult res)
        {
            string Ans = Encoding.Unicode.GetString(res.AsyncState as byte[], 0, socket.EndReceive(res));
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (Ans == "OK")
                {
                    State_lbl.Text = "OK";

                    Hide();
                    MenuWindow menuWindow = new MenuWindow(socket, Sign_Name_txtbox.Text, int.Parse(NetLib.ReadDataFromNet(socket)));
                    menuWindow.ShowDialog();
                    Close();
                }
                else if (Ans == "PassNameError")
                {
                    State_lbl.Text = "Неправильное имя или пароль. Попробуйте еще раз.";
                }
                else if (Ans == "ActivityError")
                {
                    State_lbl.Text = "Этот пользователь уже находится в сети.";
                }
            }));
        }

        private void Come_Back_btn_Click(object sender, RoutedEventArgs e)
        {
            Reg_Name_txtbox.Visibility = Visibility.Hidden;
            Reg_Pass_txtbox.Visibility = Visibility.Hidden;
            Reg_RePass_txtbox.Visibility = Visibility.Hidden;
            State_lbl.Visibility = Visibility.Hidden;
            PassName_Reg_btn.Visibility = Visibility.Hidden;
            PassName_Sign_btn.Visibility = Visibility.Hidden;
            Sign_Name_txtbox.Visibility = Visibility.Hidden;
            Sign_Pass_txtbox.Visibility = Visibility.Hidden;
            Reg_btn.Visibility = Visibility.Visible;
            Sign_btn.Visibility = Visibility.Visible;
            Come_Back_btn.Visibility = Visibility.Hidden;
        }
    }
}
