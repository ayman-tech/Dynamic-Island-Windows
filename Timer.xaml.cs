using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DynamicIsland
{
    public partial class Timer : UserControl
    {
        private DispatcherTimer _timer;
        private TimeSpan _timeLeft;
        private bool _isRunning;
        BitmapImage playImg, pauseImg;
        private readonly string curTag = "Timer".PadRight(10);
        // Only allow digits on keyboard input
        private static readonly Regex _digitsOnly = new Regex("^[0-9]+$");

        public Timer()
        {
            InitializeComponent();
            _timeLeft = TimeSpan.FromMinutes(AppSettings.Time);
            //_timeLeft = TimeSpan.FromMinutes(30);
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            playImg = new BitmapImage(new Uri("pack://application:,,,/Assets/play.png"));
            pauseImg = new BitmapImage(new Uri("pack://application:,,,/Assets/pause.png"));
        }
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_digitsOnly.IsMatch(e.Text);
        }

        // Block paste operations with non-digits
        private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = e.DataObject.GetData(DataFormats.Text) as string;
                if (!_digitsOnly.IsMatch(text ?? ""))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_timeLeft.TotalSeconds > 0)
                {
                    _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
                    MinutesBox.Text = _timeLeft.ToString("mm");
                    SecondsBox.Text = _timeLeft.ToString("ss");
                    //TimerText.Text = _timeLeft.ToString(@"mm\:ss");
                }
                else
                {
                    _timer.Stop(); 
                    playPauseImg.Source = playImg;
                    Ilog.Info(curTag, "End Timer");
                    SystemSounds.Beep.Play();
                    MessageBox.Show("Time’s up! ⏰ Ready for the next round?", "Timer Timeout");
                    _isRunning = false;
                }
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void PausePlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isRunning)
                {
                    _timer.Stop();
                    //PausePlayButton = "Start";
                    playPauseImg.Source = playImg;
                    Ilog.Info(curTag, "Pause Timer");
                }
                else if (int.TryParse(MinutesBox.Text, out var m) && int.TryParse(SecondsBox.Text, out var s) && m < 60 && s < 60)
                {
                    _timeLeft = TimeSpan.FromMinutes(m) + TimeSpan.FromSeconds(s);
                    _timer.Start();
                    //PausePlayButton.Content = "Pause";
                    playPauseImg.Source = pauseImg;
                    Ilog.Info(curTag, "Play Timer");
                }
                else
                {
                    MessageBox.Show("Enter 0–59 for both minutes and seconds.");
                }

                _isRunning = !_isRunning;
            }
            catch(Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void PausePlayButton_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(int.TryParse(MinutesBox.Text, out var m) && m > 0)
            {
                AppSettings.Time = m;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                Ilog.Info(curTag, "Reset Timer");
                _timer.Stop();
                _timeLeft = TimeSpan.FromMinutes(AppSettings.Time);
                MinutesBox.Text = _timeLeft.ToString("mm");
                SecondsBox.Text = _timeLeft.ToString("ss");
                //TimerText.Text = _timeLeft.ToString(@"mm\:ss");
                playPauseImg.Source = playImg;
                _isRunning = false;
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }
    }
}
