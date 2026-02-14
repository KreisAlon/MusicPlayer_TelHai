using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace Telhai.DotNet.PlayerProject
{
    public partial class MusicPlayer : Window
    {
        // Core components for the media player and track list
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";

        // Requirement 3.2: Components for the image slideshow
        private DispatcherTimer slideshowTimer = new DispatcherTimer();
        private List<string> currentTrackImages = new List<string>();
        private int currentImageIndex = 0;

        // Requirement 2: Service for fetching song details from iTunes API
        private ItunesService apiService = new ItunesService();
        private System.Threading.CancellationTokenSource? tokenSource;

        public MusicPlayer()
        {
            InitializeComponent();

            // Progress timer: Updates the seek bar while music plays
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            // Requirement 3.2: Slideshow timer - rotates images every 3 seconds
            slideshowTimer.Interval = TimeSpan.FromSeconds(3);
            slideshowTimer.Tick += SlideshowTimer_Tick;

            this.Loaded += MusicPlayer_Loaded;
        }

        private void MusicPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLibrary(); // Loads saved tracks from JSON
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Sync the slider position with the media playback
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !isDragging)
            {
                sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderProgress.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            // Check if a song is selected in the library
            if (lstLibrary.SelectedItem is MusicTrack selectedTrack)
            {
                // 1. Check if we need to load a new file
                // We compare the current player source with the selected track's path
                bool isNewSong = mediaPlayer.Source == null ||
                                 Path.GetFullPath(mediaPlayer.Source.LocalPath) != Path.GetFullPath(selectedTrack.FilePath);

                if (isNewSong)
                {
                    // Stop whatever is playing now
                    mediaPlayer.Stop();

                    // Open the newly selected track
                    mediaPlayer.Open(new Uri(selectedTrack.FilePath));

                    // Sync the UI and Slideshow for the new song
                    ShowTrackMetadata(selectedTrack);
                }

                // 2. Start playing (either the new song or resuming the current one)
                mediaPlayer.Play();
                timer.Start(); // Start the progress slider timer
                txtStatus.Text = "Playing";
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            txtStatus.Text = "Paused";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            timer.Stop();
            slideshowTimer.Stop(); // Stop the images from rotating

            sliderProgress.Value = 0; // Reset the seek bar
            txtStatus.Text = "Stopped";
        }
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
        }

        private void Slider_DragStarted(object sender, MouseButtonEventArgs e) => isDragging = true;

        private void Slider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliderProgress.Value);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "MP3 Files|*.mp3" };

            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames)
                {
                    if (!library.Any(t => t.FilePath == file))
                    {
                        library.Add(new MusicTrack
                        {
                            Title = Path.GetFileNameWithoutExtension(file),
                            FilePath = file
                        });
                    }
                }
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                library.Remove(track);
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void UpdateLibraryUI()
        {
            lstLibrary.ItemsSource = null;
            lstLibrary.ItemsSource = library;
        }

        private void SaveLibrary()
        {
            // Requirement 3.1: Save everything to JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(library, options);
            File.WriteAllText(FILE_NAME, json);
        }

        private void LoadLibrary()
        {
            if (File.Exists(FILE_NAME))
            {
                string json = File.ReadAllText(FILE_NAME);
                library = JsonSerializer.Deserialize<List<MusicTrack>>(json) ?? new List<MusicTrack>();
                UpdateLibraryUI();
            }
        }

        // Requirement 2: Displays basic info on single click
        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                txtCurrentSong.Text = track.Title;
                txtStatus.Text = track.FilePath;

                // Requirement 3.1: Use cached data if it exists
                if (track.IsMetadataLoaded)
                {
                    ShowTrackMetadata(track);
                }
                else
                {
                    txtArtist.Text = "Play to search details";
                    imgAlbumArt.Source = null;
                    slideshowTimer.Stop();
                }
            }
        }

        // Requirement 2 & 3.1: Plays song and fetches data via API
        private async void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                mediaPlayer.Open(new Uri(track.FilePath));
                mediaPlayer.Play();
                timer.Start();
                txtStatus.Text = "Playing...";

                // Requirement 3.1: If already loaded, skip API call
                if (track.IsMetadataLoaded)
                {
                    ShowTrackMetadata(track);
                    return;
                }

                CancelActiveSearch();
                tokenSource = new System.Threading.CancellationTokenSource();
                txtArtist.Text = "Searching iTunes...";

                try
                {
                    var info = await apiService.GetSongDetails(track.Title, tokenSource.Token);

                    if (info != null)
                    {
                        track.Artist = info.ArtistName;
                        track.Album = info.CollectionName;
                        track.ImageUrl = info.ArtworkUrl100;
                        track.IsMetadataLoaded = true;

                        ShowTrackMetadata(track);
                        SaveLibrary();
                    }
                    else
                    {
                        ShowErrorInfo(track);
                    }
                }
                catch (OperationCanceledException) { }
                catch
                {
                    ShowErrorInfo(track);
                    txtStatus.Text = "API Connection Error";
                }
            }
        }

        // Requirement 3.2: Main method for UI updates and slideshow setup
        private void ShowTrackMetadata(MusicTrack track)
        {
            txtCurrentSong.Text = track.Title;
            txtArtist.Text = track.Artist;

            currentTrackImages.Clear();

            // API Image from iTunes
            if (!string.IsNullOrEmpty(track.ImageUrl))
                currentTrackImages.Add(track.ImageUrl);

            // User added images from the MVVM Edit Window
            if (track.UserImages != null && track.UserImages.Count > 0)
                currentTrackImages.AddRange(track.UserImages);

            currentImageIndex = 0;

            if (currentTrackImages.Count > 0)
            {
                UpdateImageSource(currentTrackImages[0]);

                // Requirement 3.2: Start slideshow if there are multiple images
                if (currentTrackImages.Count > 1)
                    slideshowTimer.Start();
                else
                    slideshowTimer.Stop();
            }
            else
            {
                imgAlbumArt.Source = null;
                slideshowTimer.Stop();
            }
        }

        private void SlideshowTimer_Tick(object? sender, EventArgs e)
        {
            if (currentTrackImages.Count > 1)
            {
                currentImageIndex = (currentImageIndex + 1) % currentTrackImages.Count;
                UpdateImageSource(currentTrackImages[currentImageIndex]);
            }
        }
        private void UpdateImageSource(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();

                // This handles both local paths and the single API URL
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);

                // Critical for local files: ensures they are loaded and not "locked"
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgAlbumArt.Source = bitmap;
            }
            catch { /* If a file is moved or deleted, skip it */ }
        }
        private void ShowErrorInfo(MusicTrack track)
        {
            txtCurrentSong.Text = Path.GetFileNameWithoutExtension(track.FilePath);
            txtStatus.Text = track.FilePath;
            txtArtist.Text = "Metadata not found";
        }

        private void CancelActiveSearch()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings settingsWin = new Settings();
            settingsWin.OnScanCompleted += (newTracks) => {
                foreach (var t in newTracks)
                {
                    if (!library.Any(x => x.FilePath == t.FilePath)) library.Add(t);
                }
                UpdateLibraryUI();
                SaveLibrary();
            };
            settingsWin.ShowDialog();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack selectedTrack)
            {
                // Requirement 3.2: Open MVVM Edit Window
                SongDetailsWindow editWin = new SongDetailsWindow(selectedTrack);
                if (editWin.ShowDialog() == true)
                {
                    UpdateLibraryUI();
                    SaveLibrary();
                    ShowTrackMetadata(selectedTrack); // Update UI if details changed
                }
            }
        }
    }



    //private void MusicPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    //{
    //    MainWindow p = new MainWindow();
    //    p.Title = "YYYYY";
    //    p.Show();
    //}
}