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
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using SpyGamePlus_Client.Classes;
using System.Threading;
using System.Windows.Threading;

namespace SpyGamePlus
{
    /// <summary>
    /// Логика взаимодействия для MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        int PlayerCount;
        Socket socket;
        string name;
        bool IsReceivingBegin = false;
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public MenuWindow(Socket socket, string Name, int rate)
        {
            InitializeComponent();
            this.socket = socket;
            this.name = Name;
            Name_lbl.Text = "Имя: " + Name;
            Rate_lbl.Text = "Рейтинг: " + rate;
        }

        private void Play_btn_Click(object sender, RoutedEventArgs e)
        {
            PlayerCount = 0;
            try
            {
                PlayerCount = int.Parse(Players_Count_txt.Text);
            }
            catch (Exception)
            {
                Clue_lbl.Text = "Некорректный ввод. Попробуйте ещё раз.";
                return;
            }
            if (PlayerCount >= 2 && PlayerCount <= 16)
            {
                NetLib.SendDataToNet(socket, "PlayersCount/" + PlayerCount.ToString());
                Players_Count_txt.Visibility = Visibility.Hidden;
                Play_btn.Visibility = Visibility.Hidden;
                Cancel_Waiting_btn.Visibility = Visibility.Visible;
                Clue_lbl.Text = "Поиск игры...";
                if (!IsReceivingBegin)
                {
                    byte[] Data = new byte[2];
                    socket.BeginReceive(Data, 0, Data.Length, 0, ReceiveCallback, Data);
                    IsReceivingBegin = true;
                }
            }
            else if (PlayerCount < 2 || PlayerCount > 16)
            {
                Clue_lbl.Text = "Некорректное кол-во игроков. Введите число от 4 до 16";
                return;
            }
        }
        public void ReceiveCallback(IAsyncResult result)
        {
            string mes = Encoding.Unicode.GetString(result.AsyncState as byte[]);
            if (mes == "P")
            {
                Dispatcher.Invoke(() =>
                {
                    Hide();
                    GameWindow gameWindow = new GameWindow(socket, name, PlayerCount);
                    gameWindow.Show();
                    Close();
                });
            }
        }

        private void Cancel_Waiting_btn_Click(object sender, RoutedEventArgs e)
        {
            Players_Count_txt.Visibility = Visibility.Visible;
            Play_btn.Visibility = Visibility.Visible;
            Clue_lbl.Text = "Введите кол-во игроков(4-16):";
            Cancel_Waiting_btn.Visibility = Visibility.Hidden;
            NetLib.SendDataToNet(socket, "Cancel");
        }
    }
}
