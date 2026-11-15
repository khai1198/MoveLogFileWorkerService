using FluentFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MoveLogFileWorkerService
{
  public class PostgreSqlBackupService
  {
    private readonly string _pgDumpPath;

    public PostgreSqlBackupService(string pgDumpPath)
    {
      _pgDumpPath = pgDumpPath;
    }

    public async Task<string> BackupAndUploadAsync(
        string dbHost,
        int dbPort,
        string database,
        string dbUsername,
        string dbPassword,
        string localFolder,
        string ftpHost,
        int ftpPort,
        string ftpUsername,
        string ftpPassword,
        string ftpFolder)
    {

      if (!Directory.Exists(localFolder))
      {
        Directory.CreateDirectory(localFolder);
      }

      string fileName =
          $"{database}_{DateTime.Now:yyyyMMdd_HHmmss}.backup";

      string localFile = Path.Combine(localFolder, fileName);

      var di = new DirectoryInfo(localFolder);
      foreach (FileInfo file in di.GetFiles())
      {
        file.Delete();
      }

      foreach (DirectoryInfo dir in di.GetDirectories())
      {
        dir.Delete(true);
      }

      // 1. Backup PostgreSQL
      await BackupDatabaseAsync(
          dbHost, dbPort, database,
          dbUsername, dbPassword, localFile);

      // 2. Upload FTP
      await UploadToFtpAsync(
          localFile,
          ftpHost, ftpPort,
          ftpUsername, ftpPassword,
          ftpFolder);

      return localFile;
    }

    private async Task BackupDatabaseAsync(
        string host,
        int port,
        string database,
        string username,
        string password,
        string outputFile)
    {
      var psi = new ProcessStartInfo
      {
        FileName = _pgDumpPath,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardError = true,
        RedirectStandardOutput = true
      };

      psi.ArgumentList.Add("-h");
      psi.ArgumentList.Add(host);

      psi.ArgumentList.Add("-p");
      psi.ArgumentList.Add(port.ToString());

      psi.ArgumentList.Add("-U");
      psi.ArgumentList.Add(username);

      psi.ArgumentList.Add("-d");
      psi.ArgumentList.Add(database);

      psi.ArgumentList.Add("-F");
      psi.ArgumentList.Add("c");

      psi.ArgumentList.Add("-b");
      psi.ArgumentList.Add("-v");

      psi.ArgumentList.Add("-f");
      psi.ArgumentList.Add(outputFile);

      psi.Environment["PGPASSWORD"] = password;

      using var process = new Process { StartInfo = psi };

      try
      {
        process.Start();

        var errorTask =
            process.StandardError.ReadToEndAsync();

        var outputTask =
            process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        string error = await errorTask;

        if (process.ExitCode != 0)
          throw new Exception("Backup lỗi: " + error);

        if (!File.Exists(outputFile) ||
            new FileInfo(outputFile).Length == 0)
          throw new Exception("File backup không hợp lệ.");
      }
      finally
      {
        psi.Environment.Remove("PGPASSWORD");
      }
    }

    private async Task UploadToFtpAsync(
        string localFile,
        string ftpHost,
        int ftpPort,
        string ftpUsername,
        string ftpPassword,
        string ftpFolder)
    {
      using var ftp = new AsyncFtpClient(
          ftpHost,
          ftpUsername,
          ftpPassword,
          ftpPort);

      ftp.Config.DataConnectionType =
          FtpDataConnectionType.AutoPassive;

      await ftp.Connect();

      try
      {
        await ftp.CreateDirectory(ftpFolder);

        string remoteFile =
            ftpFolder.TrimEnd('/') + "/" +
            Path.GetFileName(localFile);

        var status = await ftp.UploadFile(
            localFile,
            remoteFile,
            FtpRemoteExists.Overwrite,
            true);

        if (status != FtpStatus.Success)
          throw new Exception("Upload FTP thất bại.");

        // Kiểm tra dung lượng file trên FTP
        long remoteSize = await ftp.GetFileSize(remoteFile);
        long localSize = new FileInfo(localFile).Length;

        if (remoteSize != localSize)
          throw new Exception(
              "Dung lượng file FTP không khớp!");

      }
      finally
      {
        await ftp.Disconnect();
      }
    }
  }
}
