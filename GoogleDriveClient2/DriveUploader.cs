using DotNetEnv;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDriveClient2
{
    public class DriveUploader
    {
        private DriveService? _service;

        private async Task<DriveService> GetServiceAsync()
        {
            if (_service != null)
                return _service;

            Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

            var secrets = new ClientSecrets
            {
                ClientId = Env.GetString("GOOGLE_CLIENT_ID"),
                ClientSecret = Env.GetString("GOOGLE_CLIENT_SECRET")
            };

            // Przy pierwszym uruchomieniu otworzy przeglądarkę z logowaniem,
            // token zapisze się lokalnie i kolejne razy nie trzeba się logować.
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { DriveService.Scope.DriveFile },
                "user",
                CancellationToken.None,
                new FileDataStore(Path.Combine(FileSystem.AppDataDirectory, "google-token"), true));

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveClient2"
            });

            return _service;
        }

        public async Task<string> UploadAsync(string sciezka)
        {
            var service = await GetServiceAsync();

            var metadane = new DriveFile { Name = Path.GetFileName(sciezka) };

            await using var stream = System.IO.File.OpenRead(sciezka);
            var request = service.Files.Create(metadane, stream, "application/octet-stream");
            request.Fields = "id";

            var wynik = await request.UploadAsync();
            if (wynik.Status != UploadStatus.Completed)
                throw wynik.Exception ?? new Exception("Nie udało się wysłać pliku.");

            return request.ResponseBody.Id;
        }
    }
}