﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Downloader
{
    internal class Download : IDownload
    {
        private readonly IDownloadService downloadService;
        public string Url { get; }
        public string Folder { get; }
        public string Filename { get; }
        public long DownloadedFileSize => downloadService?.Package?.ReceivedBytesSize ?? 0;
        public long TotalFileSize => downloadService?.Package?.TotalFileSize ?? DownloadedFileSize;
        public DownloadPackage Package { get; private set; }
        public DownloadStatus Status { get; private set; }

        public event EventHandler<DownloadProgressChangedEventArgs> ChunkDownloadProgressChanged
        {
            add { downloadService.ChunkDownloadProgressChanged += value; }
            remove { downloadService.ChunkDownloadProgressChanged -= value; }
        }

        public event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted
        {
            add { downloadService.DownloadFileCompleted += value; }
            remove { downloadService.DownloadFileCompleted -= value; }
        }

        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged
        {
            add { downloadService.DownloadProgressChanged += value; }
            remove { downloadService.DownloadProgressChanged -= value; }
        }

        public event EventHandler<DownloadStartedEventArgs> DownloadStarted
        {
            add { downloadService.DownloadStarted += value; }
            remove { downloadService.DownloadStarted -= value; }
        }

        public Download(string url, string path, string filename, DownloadConfiguration configuration)
        {
            downloadService = new DownloadService(configuration);
            Url = url;
            Folder = path;
            Filename = filename;
            Package = downloadService.Package;
            Status = DownloadStatus.Created;
        }

        public Download(DownloadPackage package, DownloadConfiguration configuration)
        {
            downloadService = new DownloadService(configuration);
            Package = package;
            Status = DownloadStatus.Stopped;
        }

        public async Task<Stream> StartAsync()
        {
            Status = DownloadStatus.Running;
            try
            {
                if (string.IsNullOrWhiteSpace(Package?.Address))
                {
                    if (string.IsNullOrWhiteSpace(Folder) && string.IsNullOrWhiteSpace(Filename))
                    {
                        return await downloadService.DownloadFileTaskAsync(Url);
                    }
                    else if (string.IsNullOrWhiteSpace(Filename))
                    {
                        await downloadService.DownloadFileTaskAsync(Url, new DirectoryInfo(Folder));
                        return null;
                    }
                    else
                    {
                        // with Folder and Filename
                        await downloadService.DownloadFileTaskAsync(Url, Path.Combine(Folder, Filename));
                        return null;
                    }
                }
                else
                {
                    return await downloadService.DownloadFileTaskAsync(Package);
                }
            }
            finally
            {
                Status = downloadService.IsCancelled
                    ? DownloadStatus.Stopped
                    : downloadService.Package.IsSaveComplete
                        ? DownloadStatus.Completed
                        : DownloadStatus.Failed;
            }
        }

        public void Stop()
        {
            downloadService.CancelAsync();
            Status = DownloadStatus.Stopped;
        }

        public void Pause()
        {
            downloadService.Pause();
            Status = DownloadStatus.Paused;
        }

        public void Resume()
        {
            downloadService.Resume();
            Status = DownloadStatus.Running;
        }

        public void Clear()
        {
            Stop();
            downloadService.Clear();
            Package = null;
            Status = DownloadStatus.Created;
        }

        public override bool Equals(object obj)
        {
            return obj is Download download &&
                   GetHashCode() == download.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashCode = 37;
            hashCode = (hashCode * 7) + Url.GetHashCode();
            hashCode = (hashCode * 7) + DownloadedFileSize.GetHashCode();
            return hashCode;
        }
    }
}
