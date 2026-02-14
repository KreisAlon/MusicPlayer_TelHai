using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace Telhai.DotNet.PlayerProject
{
    // This class connects our data (MusicTrack) to the Window (UI)
    public class EditSongViewModel : INotifyPropertyChanged
    {
        private MusicTrack _track;

        // This is the title showing in the TextBox
        public string SongTitle
        {
            get => _track.Title;
            set
            {
                _track.Title = value;
                OnPropertyChanged(); // Tells the UI to update the text
            }
        }

        // This list holds all image paths/links for the slideshow
        public ObservableCollection<string> ImageList { get; set; }

        public EditSongViewModel(MusicTrack track)
        {
            _track = track;

            // We start with the images already saved in the song
            ImageList = new ObservableCollection<string>(track.UserImages);

            // If iTunes found an image, we add it to the list as well
            if (!string.IsNullOrEmpty(track.ImageUrl) && !ImageList.Contains(track.ImageUrl))
            {
                ImageList.Add(track.ImageUrl);
            }
        }

        // Method 1: Add a picture from the computer files
        public void AddImageFromFile()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg" };
            if (ofd.ShowDialog() == true)
            {
                ImageList.Add(ofd.FileName);
                _track.UserImages.Add(ofd.FileName);
            }
        }

        // Method 2: Add a picture using a web link (URL)
        public void AddImageFromUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                ImageList.Add(url);
                _track.UserImages.Add(url);
            }
        }

        // Method 3: Delete a selected image
        public void RemoveImage(string path)
        {
            if (path != null)
            {
                ImageList.Remove(path);
                _track.UserImages.Remove(path);
            }
        }

        // Standard MVVM part - ignore this, it just makes the Binding work
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}