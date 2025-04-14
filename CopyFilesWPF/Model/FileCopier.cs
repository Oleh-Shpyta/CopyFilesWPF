using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CopyFilesWPF.Model
{
    public class FileCopier
    {
        private readonly Grid _gridPanel;
        private readonly FilePath _filePath;
        private readonly CancellationTokenSource _cts = new();
        public delegate void ProgressChangeDelegate(double progress, Grid gridPanel);
        public delegate void CompleteDelegate(Grid gridPanel);
        public event ProgressChangeDelegate OnProgressChanged;
        public event CompleteDelegate OnComplete;
        public ManualResetEventSlim PauseFlag = new(true);
        public FileCopier(
            FilePath filePath,
            ProgressChangeDelegate onProgressChange,
            CompleteDelegate onComplete,
            Grid gridPanel)
        {
            OnProgressChanged += onProgressChange;
            OnComplete += onComplete;
            _filePath = filePath;
            _gridPanel = gridPanel;
        }
        public void Cancel() => _cts.Cancel();
        public void Pause() => PauseFlag.Reset();
        public void Resume() => PauseFlag.Set();
        public void CopyFile()
        {
            byte[] buffer = new byte[1024 * 1024];
            while (true)
            {
                try
                {
                    using var source = new FileStream(_filePath.PathFrom, FileMode.Open, FileAccess.Read);
                    var fileLength = source.Length;
                    using var destination = new FileStream(_filePath.PathTo, FileMode.CreateNew, FileAccess.Write);

                    long totalBytes = 0;
                    int currentBlockSize;

                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        PauseFlag.Wait();

                        totalBytes += currentBlockSize;
                        double percentage = totalBytes * 100.0 / fileLength;
                        destination.Write(buffer, 0, currentBlockSize);

                        OnProgressChanged?.Invoke(percentage, _gridPanel);
                    }
                    break;
                }
                catch (IOException error)
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        var result = MessageBox.Show(error.Message + " Replace?", "Replace?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            File.Delete(_filePath.PathTo);
                            continue;
                        }
                    }
                    else
                    {
                        MessageBox.Show(error.Message + " Copying was canceled!", "Cancel", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        File.Delete(_filePath.PathTo);
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    File.Delete(_filePath.PathTo);
                    MessageBox.Show("Copy operation was canceled.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error occurred!", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }
            OnComplete?.Invoke(_gridPanel);
        }
    }
}
