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
    /// Логика взаимодействия для GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        string[] PlayersNames;
        int TargetIndex;
        Button[] buttons;
        ManualResetEvent AllDone = new ManualResetEvent(false);
        string TargetUser;
        string UserName;
        int MyIndex;
        Socket socket;
        int PlayerCount;
        string Role;
        bool IsSpyMod;
        bool IsOkPressed;
        bool AutoClose;
        public GameWindow(Socket socket, string Name, int PlayerCount)
        {
            InitializeComponent();
            AutoClose = false;
            this.socket = socket;
            this.UserName = Name;
            this.PlayerCount = PlayerCount;
            IsSpyMod = false;
            IsOkPressed = true;
            PlayersNames = new string[PlayerCount];
            Thread FeelThread = new Thread(FeelPlayersList);
            FeelThread.IsBackground = true;
            FeelThread.Start();
            Thread GameThread = new Thread(Game);
            GameThread.IsBackground = true;
            GameThread.Start();
        }
        public void Game()
        {
            AllDone.WaitOne();
            Target_lbl.Dispatcher.Invoke(new Action(() => Target_lbl.Text = "Waiting for Role"));
            Role = NetLib.ReadDataFromNet(socket);
            Target_lbl.Dispatcher.Invoke(new Action(() => Target_lbl.Text = "mmmm, Nice"));
            if (Role == "Spy")
            {
                Role_txt.Dispatcher.Invoke(new Action(() => 
                {
                    Role_txt.Text = "Вы - шпион " + UserName + "!";
                    Action_lbl.Text = "Раскройте кодовое слово!";
                    Action_txt.IsEnabled = false;
                    Change_Mode_btn.Visibility = Visibility.Visible;
                }));
            }
            else
            {
                Role_txt.Dispatcher.Invoke(new Action(() =>
                {
                    Role_txt.Text = "Вы - мирный житель " + UserName + "!";
                    Action_lbl.Text = "Раскройте личность шпиона!";
                    Action_txt.IsEnabled = false;
                    Keyword_txt.Text = "Кодовое слово: " + NetLib.ReadDataFromNet(socket);
                }));
            }
            string[] mes = new string[1];
            string[] Command = new string[1];
            bool IsEnd = false;
            bool IsWin = true;
            while (true)
            {
                mes = NetLib.ReadDataFromNet(socket).Split('|');
                for (int i = 0; i < mes.Length - 1; i++)
                {
                    if (mes[i] == "Turn")
                    {
                        Send_Question_btn.Dispatcher.Invoke(new Action(() =>
                        {
                            Send_Question_btn.IsEnabled = true;
                        }));
                    }
                    else if (mes[i] == "Debate")
                    {
                        Send_Message_btn.Dispatcher.Invoke(new Action(() =>
                        {
                            Send_Message_btn.IsEnabled = true;
                            Action_btn.IsEnabled = true;
                            IsOkPressed = false;
                        }));
                    }
                    else if (mes[i] == "EndDebate")
                    {
                        Send_Message_btn.Dispatcher.Invoke(new Action(() =>
                        {
                            Send_Message_btn.IsEnabled = false;
                            Action_btn.IsEnabled = false;
                            IsOkPressed = true;
                        }));
                    }
                    else
                    {
                        Command = mes[i].Split('/');
                        int senderIndex = int.Parse(Command[0]);
                        int receiverIndex = int.Parse(Command[2]);
                        string order = Command[1];
                        string text = Command[3];
                        if (order == "Question")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add("Игрок " + PlayersNames[senderIndex] + " задает игроку " + PlayersNames[receiverIndex] + " вопрос: " + text + "?" + "\r\n")));
                            if (MyIndex == receiverIndex)
                            {
                                Yes_btn.Dispatcher.Invoke(new Action(() =>
                                {
                                    Yes_btn.IsEnabled = true;
                                    No_btn.IsEnabled = true;
                                }));
                            }
                        }
                        else if (order == "Answer")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add("Игрок " + PlayersNames[senderIndex] + " отвечает: " + text + "\r\n")));
                        }
                        else if (order == "Message")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add(PlayersNames[senderIndex] + ": " + text + "\r\n")));
                        }
                        else if (order == "SelectedSpy")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add("Игрок " + PlayersNames[senderIndex] + " голосует за игрока " + PlayersNames[receiverIndex] + "\r\n")));
                        }
                        else if (order == "DeadSpy")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add("Игрок " + PlayersNames[receiverIndex] + text + "\r\n");
                                buttons[receiverIndex].IsEnabled = false;
                                if (TargetUser == PlayersNames[receiverIndex])
                                {
                                    TargetUser = null;
                                    Target_lbl.Text = null;
                                }
                            }));
                            if (receiverIndex == MyIndex)
                            {
                                IsWin = false;
                                IsEnd = true;
                                break;
                            }
                        }
                        else if (order == "DeadRes")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add("Игрок " + PlayersNames[receiverIndex] + " был убит по ошибке других горожан!\r\n");
                                buttons[receiverIndex].IsEnabled = false;
                                if (TargetUser == PlayersNames[receiverIndex])
                                {
                                    TargetUser = null;
                                    Target_lbl.Text = null;
                                }
                                if (Role == "Spy")
                                {
                                    Keyword_txt.Inlines.Add(text);
                                }
                            }));
                            if (receiverIndex == MyIndex)
                            {
                                IsWin = false;
                                IsEnd = true;
                                break;
                            }
                        }
                        else if (order == "NoDead")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add("Сегодня горожане никого не убили! Мирные жители живы.\r\n");
                            }));
                        }
                        else if (order == "ResWin")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add(text + "\r\n");
                            }));
                            if (Role != "Spy")
                            {
                                IsWin = true;
                                IsEnd = true;
                            }
                            else
                            {
                                IsWin = false;
                                IsEnd = true;
                            }
                            break;
                        }
                        else if (order == "SpyWin")
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add(text + "\r\n");
                            }));
                            if (Role == "Spy")
                            {
                                IsWin = true;
                                IsEnd = true;
                            }
                            else
                            {
                                IsWin = false;
                                IsEnd = true;
                            }
                            break;
                        }
                        else if (order == "Disconnect") 
                        {
                            Chat_txt.Dispatcher.Invoke(new Action(() =>
                            {
                                Chat_txt.Inlines.Add("Игрок " + PlayersNames[senderIndex] + " отключился от игры\r\n");
                                buttons[senderIndex].IsEnabled = false;
                                if (TargetUser == PlayersNames[senderIndex])
                                {
                                    TargetUser = null;
                                    Target_lbl.Text = null;
                                }
                            }));
                        }
                    }
                }
                if (IsEnd)
                {
                    break;
                }
            }
            if (IsWin)
            {
                Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add("Вы победили!")));
                NetLib.SendDataToNet(socket, "Win");
            }
            else
            {
                Chat_txt.Dispatcher.Invoke(new Action(() => Chat_txt.Inlines.Add("Вы проиграли...")));
                NetLib.SendDataToNet(socket, "Lose");
            }
            Thread.Sleep(5000);
            this.Dispatcher.Invoke(new Action(() =>
            {
                Hide();
                MenuWindow menuWindow = new MenuWindow(socket, UserName, int.Parse(NetLib.ReadDataFromNet(socket)));
                menuWindow.ShowDialog();
                AutoClose = true;
                Close();
            }));
            
        }
        public void FeelPlayersList()
        {
            string mes = NetLib.ReadDataFromNet(socket);
            string[] PlayerListMes = mes.Split('/');
            buttons = new Button[PlayerListMes.Length];
            Players_lst.Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < PlayerCount; i++)
                {
                    Target_lbl.Text = PlayerListMes[i];
                    Button btn = new Button();
                    btn.Content = PlayerListMes[i];
                    btn.Click += UserName_btn_Click;
                    btn.Tag = i;
                    if (PlayerListMes[i] == UserName)
                    {
                        btn.IsEnabled = false;
                        MyIndex = i;
                    }
                    Players_lst.RowDefinitions.Add(new RowDefinition());
                    Grid.SetRow(btn, i);
                    Players_lst.Children.Add(btn);
                    buttons[i] = btn;
                    PlayersNames[i] = PlayerListMes[i];
                }
                AllDone.Set();
            }));
        }

        private void UserName_btn_Click(object sender, RoutedEventArgs e)
        {
            Button ClickedButton = sender as Button;
            TargetUser = ClickedButton.Content.ToString();
            TargetIndex = int.Parse(ClickedButton.Tag.ToString());
            Target_lbl.Text = "Выбранный игрок - " + TargetUser;
            if (!IsSpyMod)
            {
                Action_txt.Text = "Игрок " + TargetUser + " - шпион!";
            }
        }

        private void Send_Question_btn_Click(object sender, RoutedEventArgs e)
        {
            if (TargetUser == null)
            {
                Target_lbl.Text = "Выберите пользователя которому хотите отправить сообщение";
                return;
            }
            string FinalMessage = Message_txt.Text.Replace('/', ' ').Replace('|', ' ');
            string mes = MyIndex + "/Question/" + TargetIndex + "/" + FinalMessage + "|";
            NetLib.SendDataToNet(socket, mes);
            Send_Question_btn.IsEnabled = false;
            Message_txt.Text = "";
        }

        private void No_btn_Click(object sender, RoutedEventArgs e)
        {
            string mes = MyIndex + "/Answer/" + -1 + "/Нет!|";
            NetLib.SendDataToNet(socket, mes);
            No_btn.IsEnabled = false;
            Yes_btn.IsEnabled = false;
        }

        private void Yes_btn_Click(object sender, RoutedEventArgs e)
        {
            string mes = MyIndex + "/Answer/" + -1 + "/Да!|";
            NetLib.SendDataToNet(socket, mes);
            No_btn.IsEnabled = false;
            Yes_btn.IsEnabled = false;
        }

        private void Send_Message_btn_Click(object sender, RoutedEventArgs e)
        {
            string FinalMessage = Message_txt.Text.Replace('/', ' ').Replace('|', ' ');
            string mes = MyIndex + "/Message/" + -1 + "/" + FinalMessage + "|";
            NetLib.SendDataToNet(socket, mes);
            Message_txt.Text = "";
        }

        private bool AutoScroll;
        private void Scroll_Chat_scv_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                if (Scroll_Chat_scv.VerticalOffset == Scroll_Chat_scv.ScrollableHeight)
                {
                    AutoScroll = true;
                }
                else
                {
                    AutoScroll = false;
                }
            }

            if (AutoScroll && e.ExtentHeightChange != 0)
            {
                Scroll_Chat_scv.ScrollToVerticalOffset(Scroll_Chat_scv.ExtentHeight);
            }
        }

        private void Action_btn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSpyMod)
            {
                string FinalMessage = Action_txt.Text.Replace('/', ' ').Replace('|', ' ');
                string mes = MyIndex + "/SelectedKeyWord/" + -1 + "/" + FinalMessage;
                NetLib.SendDataToNet(socket, mes);
            }
            else
            {
                IsOkPressed = true;
                if (TargetUser == null)
                {
                    Target_lbl.Text = "Выберите пользователя за которого голосуете";
                    return;
                }
                string mes = MyIndex + "/SelectedSpy/" + TargetIndex + "/NULL|";
                NetLib.SendDataToNet(socket, mes);
                Action_txt.Text = "";
                Action_btn.IsEnabled = false;
            }
        }

        private void Message_txt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TargetUser == null)
            {
                Target_lbl.Text = "Выберите пользователя чтобы вставить его имя";
                return;
            }
            Message_txt.Text += TargetUser;
        }

        private void Change_Mode_btn_Click(object sender, RoutedEventArgs e)
        {
            Action_txt.Text = "";
            TargetUser = null;
            Target_lbl.Text = "";
            if (IsSpyMod)
            {
                Action_txt.IsEnabled = false;
                Change_Mode_btn.Content = "Назвать слово";
                Action_lbl.Text = "Проголосуйте чтобы отвести подозрения";
                if (IsOkPressed)
                {
                    Action_btn.IsEnabled = false;
                }
                IsSpyMod = false;
            }
            else
            {
                Change_Mode_btn.Content = "Проголосовать";
                Action_btn.IsEnabled = true;
                Action_txt.IsEnabled = true;
                Action_lbl.Text = "Введите кодовое слово";
                IsSpyMod = true;
            }
        }

        private void On_Window_Closed(object sender, EventArgs e)
        {
            if (!AutoClose)
            {
                Environment.Exit(Environment.ExitCode);
            }
        }
    }
}
