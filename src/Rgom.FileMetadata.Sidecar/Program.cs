using CommandLine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Rgom.FileMetadata.Sidecar
{
	class Program
	{
		private static BaseOptions commandLineOptions;

		private static ConcurrentQueue<FileInfo> discoveredFiles = new ConcurrentQueue<FileInfo>();
		private static bool isFileDiscoveryComplete = false;

		private static long totalFileCount = 0;
		private static long processedfileCount = 0;
		private static long errorCount = 0;
		private static DateTime startTime;

		class BaseOptions
		{
			[Option('p', "path", Required = true, HelpText = "Set the input folder to scan.")]
			public string Path { get; set; }

			[Option('t', "threads", HelpText = "Number of threads to use for processing.", Default = 8)]
			public int ThreadCount { get; set; }
		}

		[Verb("create", HelpText = "Create file attributes metadata files.")]
		class CreateOptions : BaseOptions
		{
		}

		[Verb("restore", HelpText = "Restore file attributes.")]
		class RestoreOptions : BaseOptions
		{
			[Option('d', "delete", Required = false, HelpText = "Delete the source metadata file.")]
			public bool Delete { get; set; }
		}

		struct Metadata
		{
			[JsonPropertyName("c")]
			public DateTime CreationTimeUtc { get; set; }
			[JsonPropertyName("w")]
			public DateTime LastWriteTimeUtc { get; set; }
			[JsonPropertyName("a")]
			public DateTime LastAccessTimeUtc { get; set; }
		}

		static int Main(string[] args)
		{
			commandLineOptions = Parser.Default.ParseArguments<CreateOptions, RestoreOptions>(args)
				.MapResult(
					(CreateOptions options) => GetOptions(options),
					(RestoreOptions options) => GetOptions(options),
					errs => new BaseOptions()
				);

			if (!Directory.Exists(commandLineOptions.Path))
			{
				Console.WriteLine("Path does not exist.");
				return 1;
			}

			List<Task> tasks;

			switch (commandLineOptions)
			{
				case CreateOptions c:
					tasks = new List<Task>
					{
							Task.Factory.StartNew(() => DiscoverFiles("*", commandLineOptions.Path)),
							Task.Factory.StartNew(() => DisplayOutput())
					};

					tasks.AddRange(GetProcessTasks(commandLineOptions.ThreadCount, ProcessCreateFiles));

					break;
				case RestoreOptions r:
					tasks = new List<Task>
					{
							Task.Factory.StartNew(() => DiscoverFiles("*.meta", commandLineOptions.Path)),
							Task.Factory.StartNew(() => DisplayOutput())
					};

					tasks.AddRange(GetProcessTasks(commandLineOptions.ThreadCount, ProcessRestoreFiles));

					break;
				default:
					return 1;
			}

			try
			{
				startTime = DateTime.Now;
				Console.WriteLine($"Starting: {startTime}");

				Task.WaitAll(tasks.ToArray());

				Console.WriteLine($"Done: {DateTime.Now}");
				
				return 0;
			}
			catch (Exception ex)
			{
				// Log exception.
			}

			return 1;
		}

		private static BaseOptions GetOptions(BaseOptions options)
		{
			return options;
		}

		private static void DisplayOutput()
		{
			var delay = TimeSpan.FromSeconds(1);

			string line = "";
			string backup = "";

			do
			{
				if (totalFileCount > 0)
				{
					backup = new string('\b', line.Length);
					Console.Write(backup);
					line = $"total: {totalFileCount}  processed: {processedfileCount}  error: {errorCount}  remaining: {totalFileCount - (errorCount + processedfileCount)}  percent: {Math.Round((decimal)(processedfileCount + errorCount) / totalFileCount * 100, 2):00.00}%  elapsed: {DateTime.Now - startTime:hh\\:mm\\:ss} ";
					Console.Write(line);
				}

				Thread.Sleep(delay);

			} while (!isFileDiscoveryComplete || discoveredFiles.Count > 0);

			if (totalFileCount > 0)
			{
				backup = new string('\b', line.Length);
				Console.Write(backup);
				line = $"total: {totalFileCount}  processed: {processedfileCount}  error: {errorCount}  remaining: {totalFileCount - (errorCount + processedfileCount)}  percent: {Math.Round((decimal)(processedfileCount + errorCount) / totalFileCount * 100, 2):00.00}%  elapsed: {DateTime.Now - startTime:hh\\:mm\\:ss} ";
				Console.Write(line);
			}

			Console.WriteLine();
		}

		private static void DiscoverFiles(string searchPattern, string path)
		{
			var directoryInfo = new DirectoryInfo(path);

			foreach (var fileInfo in directoryInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories))
			{
				discoveredFiles.Enqueue(fileInfo);
				totalFileCount++;
			}

			isFileDiscoveryComplete = true;
		}

		private static List<Task> GetProcessTasks(int quantity, Action task)
		{
			var result = new List<Task>();

			for (int i = 0; i < quantity; i++)
			{
				result.Add(Task.Factory.StartNew(task));
			}

			return result;
		}

		private static void ProcessCreateFiles()
		{
			while (!isFileDiscoveryComplete || discoveredFiles.Count > 0)
			{
				if (discoveredFiles.TryDequeue(out var fileInfo))
				{
					try
					{
						var metadata = new Metadata
						{
							CreationTimeUtc = fileInfo.CreationTimeUtc,
							LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
							LastAccessTimeUtc = fileInfo.LastAccessTimeUtc
						};

						File.WriteAllText($"{fileInfo.FullName}.meta", JsonSerializer.Serialize(metadata));

						Interlocked.Increment(ref processedfileCount);
					}
					catch (Exception ex)
					{
						Interlocked.Increment(ref errorCount);
					}
				}
			}
		}

		private static void ProcessRestoreFiles()
		{
			var scopedOptions = (RestoreOptions)commandLineOptions;

			while (!isFileDiscoveryComplete || discoveredFiles.Count > 0)
			{
				if (discoveredFiles.TryDequeue(out var fileInfo))
				{
					try
					{
						var metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(fileInfo.FullName));
						var targetFileName = fileInfo.FullName.Remove(fileInfo.FullName.Length - 5);

						File.SetCreationTimeUtc(targetFileName, metadata.CreationTimeUtc);
						File.SetLastWriteTimeUtc(targetFileName, metadata.LastWriteTimeUtc);
						File.SetLastAccessTimeUtc(targetFileName, metadata.LastAccessTimeUtc);

						if (scopedOptions.Delete)
						{
							File.Delete(fileInfo.FullName);
						}

						Interlocked.Increment(ref processedfileCount);
					}
					catch (Exception ex)
					{
						Interlocked.Increment(ref errorCount);
					}
				}
			}
		}
	}
}
