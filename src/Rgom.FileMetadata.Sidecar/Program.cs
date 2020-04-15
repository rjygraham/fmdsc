using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Rgom.FileMetadata.Sidecar
{
	class Program
	{
		private static HashSet<string> files;
		private static int currentCounter = 0;

		[Verb("create", HelpText = "Create file attributes metadata files.")]
		class ExtractOptions
		{
			[Option('p', "path", Required = true, HelpText = "Set the input folder to scan.")]
			public string Path { get; set; }
		}

		[Verb("restore", HelpText = "Restore file attributes.")]
		class RestoreOptions
		{
			[Option('p', "path", Required = true, HelpText = "Set the input folder to scan.")]
			public string Path { get; set; }

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
			var result = Parser.Default.ParseArguments<ExtractOptions, RestoreOptions>(args)
			.MapResult(
			  (ExtractOptions opts) => RunExtractAndReturnExitCode(opts),
			  (RestoreOptions opts) => RunRestoreAndReturnExitCode(opts),
			  errs => 1);

			return result;
		}

		private static int RunExtractAndReturnExitCode(ExtractOptions options)
		{
			if (!ValidateInputPath(options.Path))
			{
				return -1;
			}

			Console.WriteLine($"Recursivle processing all files in: {options.Path}");

			files = new HashSet<string>(Directory.GetFiles(options.Path, "*", SearchOption.AllDirectories));

			Parallel.ForEach(files, file =>
			{
				try
				{
					var fileInfo = new FileInfo(file);

					Console.WriteLine($"Processing: {fileInfo.FullName}");

					var metadata = new Metadata
					{
						CreationTimeUtc = fileInfo.CreationTimeUtc,
						LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
						LastAccessTimeUtc = fileInfo.LastAccessTimeUtc
					};

					File.WriteAllText($"{fileInfo.FullName}.meta", JsonSerializer.Serialize(metadata));

					Interlocked.Increment(ref currentCounter);
				}
				catch (Exception ex)
				{
					Console.WriteLine();
					Console.WriteLine($"ERROR: {file}, MESSAGE: {ex.Message}");
					Console.WriteLine();
				}
			});

			Console.WriteLine($"{currentCounter} / {files.Count} complete!");

			return 0;
		}

		private static int RunRestoreAndReturnExitCode(RestoreOptions options)
		{
			if (!ValidateInputPath(options.Path))
			{
				return -1;
			}

			Console.WriteLine($"Recursivle processing all files in: {options.Path}");

			files = new HashSet<string>(Directory.GetFiles(options.Path, "*.meta", SearchOption.AllDirectories));

			Parallel.ForEach(files, file  =>
			{
				try
				{
					var fileInfo = new FileInfo(file);

					Console.WriteLine($"Processing: {fileInfo.FullName}");

					var metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(fileInfo.FullName));
					var targetFileName = fileInfo.FullName.Remove(fileInfo.FullName.Length - 5);

					File.SetCreationTimeUtc(targetFileName, metadata.CreationTimeUtc);
					File.SetLastWriteTimeUtc(targetFileName, metadata.LastWriteTimeUtc);
					File.SetLastAccessTimeUtc(targetFileName, metadata.LastAccessTimeUtc);

					if (options.Delete)
					{
						File.Delete(fileInfo.FullName);
					}

					Interlocked.Increment(ref currentCounter);
				}
				catch (Exception ex)
				{
					Console.WriteLine();
					Console.WriteLine($"ERROR: {file}, MESSAGE: {ex.Message}");
					Console.WriteLine();
				}
			});

			Console.WriteLine($"{currentCounter} / {files.Count} complete!");

			return 0;
		}

		private static bool ValidateInputPath(string path)
		{
			return Directory.Exists(path);
		}
	}
}
