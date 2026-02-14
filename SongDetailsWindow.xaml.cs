using System.Windows;

namespace Telhai.DotNet.PlayerProject
{
    public partial class SongDetailsWindow : Window
    {
        private MusicTrack trackToEdit;

        public SongDetailsWindow(MusicTrack track)
        {
            InitializeComponent();
            trackToEdit = track;

            // Load values to text boxes
            txtTitle.Text = track.Title;
            txtArtist.Text = track.Artist;
            txtAlbum.Text = track.Album;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Save values back to track
            trackToEdit.Title = txtTitle.Text;
            trackToEdit.Artist = txtArtist.Text;
            trackToEdit.Album = txtAlbum.Text;

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}