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

        public async Task<string> UploadAsync(string sciezka, string? parentId = null)
        {
            var service = await GetServiceAsync();

            var metadane = new DriveFile { 
                Name = Path.GetFileName(sciezka.TrimEnd('\\','/')), 
                Parents = parentId == null ? null : new List<string> {parentId} 
            };

            await using var stream = System.IO.File.OpenRead(sciezka);
            var request = service.Files.Create(metadane, stream, "application/octet-stream");
            
            request.Fields = "id";

            var wynik = await request.UploadAsync();
            if (wynik.Status != UploadStatus.Completed)
                throw wynik.Exception ?? new Exception("Nie udało się wysłać pliku.");

            return request.ResponseBody.Id;
        }
        private async Task<string> CreateFolder(
        string folderName,
        string parentId)
        {
            var service = await GetServiceAsync();

            var metadata = new DriveFile
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = parentId == null
                    ? null
                    : new[] { parentId }
            };

            var request = service.Files.Create(metadata);
            request.Fields = "id";

            var folder = await request.ExecuteAsync();

            return folder.Id;
        }
        public async Task UploadFolderAsync(string sciezka, string? parentId = null)
        {
            var service = await GetServiceAsync();

            var folderId = await CreateFolder(
                Path.GetFileName(sciezka.TrimEnd('\\','/')),
                parentId);

            foreach (var file in Directory.GetFiles(sciezka))
            {
                await UploadAsync(file, folderId);
            }
            foreach (var underFile in Directory.GetDirectories(sciezka))
            {
                await UploadFolderAsync(underFile, folderId);
            }



        }
    }
}