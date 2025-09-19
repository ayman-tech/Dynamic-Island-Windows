using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;

namespace DynamicIsland
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MediaControl : UserControl
    {
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties;
        //private DispatcherTimer _sessionCheckTimer;
        private BitmapImage playImg, pauseImg;
        private readonly string curTag = "MediaCtrl".PadRight(10);

        public MediaControl()
        {
            try { 
                InitializeComponent();

                _currentSession = null;
                playImg = new BitmapImage(new Uri("pack://application:,,,/Assets/play.png"));
                pauseImg = new BitmapImage(new Uri("pack://application:,,,/Assets/pause.png"));

                InitializeMediaSession();

                // Initialize and start the session check timer
                //_sessionCheckTimer = new DispatcherTimer
                //{
                //    Interval = TimeSpan.FromSeconds(10) // Check every 10 seconds
                //};
                //_sessionCheckTimer.Tick += SessionCheckTimer_Tick;
                //_sessionCheckTimer.Start();
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private async void InitializeMediaSession()
        {
            await UpdateCurrentSession();
        }

        //private async void SessionCheckTimer_Tick(object sender, EventArgs e)
        //{
        //    await UpdateCurrentSession();
        //}

        private async Task UpdateCurrentSession()
        {
            try
            {
                var sessions = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var newSession = sessions.GetCurrentSession();
                if (newSession != null && newSession != _currentSession)
                {
                    if (_currentSession != null)
                    {
                        _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                        _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                    }

                    _currentSession = newSession;
                    _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;

                    // Initialize button icon based on current playback status
                    UpdatePlayPauseButtonIcon(_currentSession.GetPlaybackInfo().PlaybackStatus);
                    // Initialize title based on current playback info
                    UpdateTitle();
                    Ilog.Info(curTag, "Current Session Updated");
                }
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            // Handle media properties changes if needed
            UpdateTitle();
        }

        private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try { 
                var playbackInfo = sender.GetPlaybackInfo();
                // Update the button icon based on playback status
                Dispatcher.Invoke(() => UpdatePlayPauseButtonIcon(playbackInfo.PlaybackStatus));
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void UpdatePlayPauseButtonIcon(GlobalSystemMediaTransportControlsSessionPlaybackStatus playbackStatus)
        {
            try {
                if (playbackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    // Set the icon to "Pause"
                    playPauseImg.Source = pauseImg;
                }
                else
                {
                    // Set the icon to "Play"
                    playPauseImg.Source = playImg;
                }
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            FullTextPopup.IsOpen = false;
            try
            {
                if (_currentSession != null)
                {
                    var playbackInfo = _currentSession.GetPlaybackInfo();
                    if (playbackInfo.Controls.IsPauseEnabled && playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        await _currentSession.TryPauseAsync();
                    }
                    else if (playbackInfo.Controls.IsPlayEnabled)
                    {
                        await _currentSession.TryPlayAsync();
                    }
                }
                else
                {
                    Ilog.Info(curTag, "Current Session null, updating it");
                    //MessageBox.Show("No media session found.");
                    await UpdateCurrentSession();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void RewindButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSession == null)
                    throw new NullReferenceException("_currentSession is null");
                // Get the current playback position
                var timelineProperties = _currentSession.GetTimelineProperties();
                var currentPosition = timelineProperties.Position;

                // Rewind 10 seconds
                var newPosition = currentPosition - TimeSpan.FromSeconds(10);

                // Change the playback position
                await _currentSession.TryChangePlaybackPositionAsync(newPosition.Ticks);
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }
        private async void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSession == null)
                    throw new NullReferenceException("_currentSession is null");
                // Get the current playback position
                var timelineProperties = _currentSession.GetTimelineProperties();
                var currentPosition = timelineProperties.Position;

                // Rewind 10 seconds
                var newPosition = currentPosition + TimeSpan.FromSeconds(10);

                // Change the playback position
                await _currentSession.TryChangePlaybackPositionAsync(newPosition.Ticks);
            }
            catch (Exception ex)
            {
                Ilog.Error(curTag, ex.Message);
            }
        }

        private void ShowFullTextPopup(object sender, MouseButtonEventArgs e)
        {
            FullTextPopup.IsOpen = !FullTextPopup.IsOpen;
        }

        private void FullTextPopup_LostFocus(object sender, RoutedEventArgs e)
        {
            FullTextPopup.IsOpen = false;
        }
        private async void UpdateTitle()
        {
            string newTitle, newFullTitle;
            try
            {
                if (_currentSession == null)
                {
                    throw new NullReferenceException("_currentSession is null");
                }
                mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();
                newFullTitle = mediaProperties.Title;
                newTitle = mediaProperties.Title;
                Ilog.Info(curTag, "Media Title updated");
                // title - video title
                // artist - youtube channel name
                // albumArtist, albumTitle - blank
            }
            catch (Exception ex)
            {
                if (ex is not NullReferenceException)
                    Ilog.Error(curTag, ex.Message);
                newFullTitle = "Play a media to display Media Info";
                newTitle = "Media";
            }

            // Update title in UI Thread
            try
            {
                Dispatcher.Invoke(() =>
                {
                    MusicFullTitle.Text = newFullTitle;
                    MusicText.Text = newTitle;
                });
            }
            catch (Exception ex2)
            {
                Ilog.Error(curTag, "Error in changing text from UI thread. " + ex2.Message);
            }
        }
    }
}
