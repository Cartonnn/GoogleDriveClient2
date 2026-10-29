using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Collections.ObjectModel;

namespace GoogleDriveClient2
{
    public partial class MainPage : ContentPage
    {
        private readonly DriveUploader _uploader = new();
        private List<FileResult> wybranePliki = new();
        private List<string> wybraneFoldery = new();
        List<string> Adresy = new List<string>();
        ObservableCollection<string> Nazwy = new ObservableCollection<string>();
        public string Sciezka { set; get; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            NazwyView.ItemsSource = Nazwy;
        }

        private void AktualizujSciezke()
        {
            var linie = wybranePliki.Select(p => p.FullPath)
                .Concat(wybraneFoldery);

            Sciezka = string.Join(Environment.NewLine, linie);
            SciezkaLabel.Text = Sciezka;
        }

        async private void DodajPliki(object sender, EventArgs e)
        {
            try
            {
                var wyniki = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Wybierz pliki"
                });

                if (wyniki == null || !wyniki.Any())
                    return;

                wybranePliki = wyniki.ToList();
                AktualizujSciezke();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", ex.Message, "ok");
            }
        }

        async private void DodajFolder(object sender, EventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeFilter.Add("*");

                var okno = Application.Current.Windows[0].Handler.PlatformView as Microsoft.Maui.MauiWinUIWindow;
                WinRT.Interop.InitializeWithWindow.Initialize(picker, okno.WindowHandle);

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                    return;

                if (!wybraneFoldery.Contains(folder.Path))
                    wybraneFoldery.Add(folder.Path);

                AktualizujSciezke();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", ex.Message, "ok");
            }
        }

        /*
    async private void DodajDoDysku(object sender, EventArgs e)
    {
        if (!wybranePliki.Any() && !wybraneFoldery.Any())
        {
            await DisplayAlert("Błąd", "Najpierw wybierz plik lub folder", "ok");
            return;
        }

        foreach (var plik in wybranePliki)
        {
            Adresy.Add(plik.FullPath);
            Nazwy.Add(plik.FileName);
        }

        foreach (var folder in wybraneFoldery)
        {
            Adresy.Add(folder);
            Nazwy.Add("[Folder] " + System.IO.Path.GetFileName(folder.TrimEnd('\\', '/')));
        }

        wybranePliki.Clear();
        wybraneFoldery.Clear();
        AktualizujSciezke();

    }
        */
        async private void DodajDoDysku(object sender, EventArgs e)
        {
            if (!wybranePliki.Any())
            {
                await DisplayAlert("Błąd", "Najpierw wybierz plik (foldery nie są jeszcze obsługiwane)", "ok");
                return;
            }

            var przycisk = (Button)sender;
            przycisk.IsEnabled = false;

            try
            {
                foreach (var plik in wybranePliki.ToList())
                {
                    try
                    {
                        var id = await _uploader.UploadAsync(plik.FullPath);

                        Adresy.Add(plik.FullPath);
                        Nazwy.Add(plik.FileName);
                        wybranePliki.Remove(plik);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Błąd", $"{plik.FileName}: {ex.Message}", "ok");
                    }
                }

                AktualizujSciezke();
            }
            finally
            {
                przycisk.IsEnabled = true;
            }
        }
    }
}