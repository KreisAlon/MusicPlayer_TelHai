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
        // Global variables for the player logic
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";

        // Services for API and task cancellation
        private ItunesService apiService = new ItunesService();
        private System.Threading.CancellationTokenSource? tokenSource;

        public MusicPlayer()
        {
            InitializeComponent();

            // Setting up the timer for the seek bar
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            this.Loaded += MusicPlayer_Loaded;
        }

        private void MusicPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLibrary();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Update the slider position while the song is playing
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !isDragging)
            {
                sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderProgress.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        // --- Basic Player Controls ---
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
            timer.Start();
            txtStatus.Text = "Playing";
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
            sliderProgress.Value = 0;
            txtStatus.Text = "Stopped";
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
        }

        private void Slider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void Slider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliderProgress.Value);
        }

        // --- Library Logic (Add/Remove/Save/Load) ---
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "MP3 Files|*.mp3" };

            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames)
                {
                    // Add only if the file isn't already in the list
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
            // Saving the library to JSON to keep changes (Requirement 3.1)
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

        // --- Requirement Logic (Selection and API) ---

        // This runs on a single click (Requirement 2)
        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                // Show Title and File Path as required
                txtCurrentSong.Text = track.Title;
                txtStatus.Text = track.FilePath;

                // Requirement 3.1: If we have cached metadata, show it without calling the API
                if (track.IsMetadataLoaded)
                {
                    UpdateGuiWithMetadata(track);
                }
                else
                {
                    txtArtist.Text = "Play to find artist info";
                    imgAlbumArt.Source = null;
                }
            }
        }

        // This runs on Double Click or Play (Requirement 2 & 3.1)
        private async void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                // Play music immediately so the user doesn't wait
                mediaPlayer.Open(new Uri(track.FilePath));
                mediaPlayer.Play();
                timer.Start();
                txtStatus.Text = "Playing...";

                // Requirement 3.1: Skip API call if data is already loaded in JSON
                if (track.IsMetadataLoaded)
                {
                    UpdateGuiWithMetadata(track);
                    return;
                }

                // If no data, start API search
                CancelActiveSearch();
                tokenSource = new System.Threading.CancellationTokenSource();

                txtArtist.Text = "Searching metadata...";

                try
                {
                    // Fetch data from iTunes using the title
                    var info = await apiService.GetSongDetails(track.Title, tokenSource.Token);

                    if (info != null)
                    {
                        // Save metadata to the track object
                        track.Artist = info.ArtistName;
                        track.Album = info.CollectionName;
                        track.ImageUrl = info.ArtworkUrl100;
                        track.IsMetadataLoaded = true; // Mark as done for next time

                        UpdateGuiWithMetadata(track);
                        SaveLibrary(); // Persist changes to JSON
                    }
                    else
                    {
                        ShowErrorInfo(track);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Search was cancelled because the user switched songs
                }
                catch
                {
                    ShowErrorInfo(track);
                    txtStatus.Text = "API Connection Error";
                }
            }
        }

        // Update the screen with artist and album image
        private void UpdateGuiWithMetadata(MusicTrack track)
        {
            txtCurrentSong.Text = track.Title;
            txtArtist.Text = track.Artist;

            if (!string.IsNullOrEmpty(track.ImageUrl))
            {
                imgAlbumArt.Source = new BitmapImage(new Uri(track.ImageUrl));
            }
        }

        // Requirement 2: What to show if the API fails
        private void ShowErrorInfo(MusicTrack track)
        {
            txtCurrentSong.Text = Path.GetFileNameWithoutExtension(track.FilePath);
            txtStatus.Text = track.FilePath;
            txtArtist.Text = "No Info Found";
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

        // --- Windows and Events ---
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
                // Open the Edit window (Requirement 3.2 - MVVM will be handled here)
                SongDetailsWindow editWin = new SongDetailsWindow(selectedTrack);
                if (editWin.ShowDialog() == true)
                {
                    UpdateLibraryUI();
                    SaveLibrary();
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