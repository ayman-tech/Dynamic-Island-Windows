using System;
using System.Windows;
using System.Windows.Controls;
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

        public Timer()
        {
            InitializeComponent();
            _timeLeft = TimeSpan.FromMinutes(30);
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            playImg = new BitmapImage(new Uri("pack://application:,,,/Assets/play.png"));
            pauseImg = new BitmapImage(new Uri("pack://application:,,,/Assets/pause.png"));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_timeLeft.TotalSeconds > 0)
                {
                    _timeLeft = _timeLeft.Add(TimeSpan.FromSeconds(-1));
                    TimerText.Text = _timeLeft.ToString(@"mm\:ss");
                }
                else
                {
                    _timer.Stop(); 
                    playPauseImg.Source = playImg;
                    Ilog.Info(curTag, "End Timer");
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
                else
                {
                    _timer.Start();
                    //PausePlayButton.Content = "Pause";
                    playPauseImg.Source = pauseImg;
                    Ilog.Info(curTag, "Play Timer");
                }

                _isRunning = !_isRunning;
            }
            catch(Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                Ilog.Info(curTag, "Reset Timer");
                _timer.Stop();
                _timeLeft = TimeSpan.FromMinutes(30);
                TimerText.Text = _timeLeft.ToString(@"mm\:ss");
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
