using System.Windows;

namespace Telhai.DotNet.PlayerProject
{
    public partial class SongDetailsWindow : Window
    {
        private EditSongViewModel viewModel;

        public SongDetailsWindow(MusicTrack track)
        {
            InitializeComponent();
            // Connect the ViewModel for MVVM
            viewModel = new EditSongViewModel(track);
            this.DataContext = viewModel;
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e) => viewModel.AddImageFromFile();


        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstImages.SelectedItem is string path) viewModel.RemoveImage(path);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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