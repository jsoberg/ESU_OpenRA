using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Esu
{
    /** A no-graphics implementation of the mod packages downloader, which auto download Red Alert. */
    public class AutoDownloadRedAlertPackagesLogic
    {
        public static readonly string MIRRORS_SERVER = "http://www.openra.net/packages/ra-mirrors.txt";
        public static readonly string RED_ALERT = "ra";

        readonly Action afterInstall;
        string mirror;

        [ObjectCreator.UseCtor]
        public AutoDownloadRedAlertPackagesLogic(Action afterInstall)
        {
            this.afterInstall = afterInstall;

            var text = "Downloading {0} assets...".F(ModMetadata.AllMods[RED_ALERT].Title);
            Console.WriteLine(text);

            BeginDownloadAndExtract();
        }

        void BeginDownloadAndExtract()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var dest = Platform.ResolvePath("^", "Content", RED_ALERT);

            Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
            {
                /* Do nothing for now. */
            };

            Action<string> onExtractProgress = i =>
            {
                /* Do nothing for now. */
            };

            Action<string> onError = s => Game.RunAfterTick(() =>
            {
                Console.WriteLine("Error: " + s);
                throw new SystemException("Error: " + s);
            });

            Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
            {
                if (i.Error != null)
                {
                    onError(Download.FormatErrorMessage(i.Error));
                    return;
                }

                if (cancelled)
                {
                    onError("Download cancelled");
                    return;
                }

                // Automatically extract
                Console.WriteLine("Extracting Red Alert assets...");
                if (InstallUtils.ExtractZip(file, dest, onExtractProgress, onError))
                {
                    Game.RunAfterTick(() =>
                    {
                        Ui.CloseWindow();
                        afterInstall();
                    });
                }
            };

            Action<DownloadDataCompletedEventArgs, bool> onFetchMirrorsComplete = (i, cancelled) =>
            {
                if (i.Error != null)
                {
                    onError(Download.FormatErrorMessage(i.Error));
                    return;
                }

                if (cancelled)
                {
                    onError("Download cancelled");
                    return;
                }

                var data = Encoding.UTF8.GetString(i.Result);
                var mirrorList = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                mirror = mirrorList.Random(new MersenneTwister());

                // Save the package to a temp file
                var dl = new Download(mirror, file, onDownloadProgress, onDownloadComplete);
            };

            // Get the list of mirrors
            var updateMirrors = new Download(MIRRORS_SERVER, onDownloadProgress, onFetchMirrorsComplete);
        }
    }
}
