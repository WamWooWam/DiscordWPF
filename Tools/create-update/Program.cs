using DiscordWPF.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace CreateUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var time = DateTime.Now;
                var inDir = args[0];
                var outDir = args[1];
                var mainExeName = args[2];
                var description = string.Join(" ", args.Skip(3));

                if (File.Exists(description))
                {
                    Console.WriteLine("Loading changelog from file"); 
                    description = File.ReadAllText(description);
                }

                Console.WriteLine("Preparing to create update package...");
                Console.ReadKey();

                try
                {
                    var x86 = Path.Combine(inDir, "x86", "Release");
                    var x64 = Path.Combine(inDir, "x64", "Release");
                    if (!Directory.Exists(x86) || !Directory.Exists(x64))
                    {
                        Console.WriteLine("Required folders are missing! Please build x86 and x64 builds before creating an update package.");
                        return;
                    }

                    Console.WriteLine("Folders verified!");

                    List<UpdateDetails> updateDetails = new List<UpdateDetails>();
                    if (File.Exists(Path.Combine(outDir, "versions.json")))
                    {
                        updateDetails = JsonConvert.DeserializeObject<List<UpdateDetails>>(File.ReadAllText(Path.Combine(outDir, "versions.json")));
                        Console.WriteLine("Loaded existing versions.json");
                    }
                    else
                    {
                        Console.WriteLine("Creating new versions.json");
                    }

                    var arr = new Task[2];

                    CreateUpdateTask(time, "x86", outDir, mainExeName, description, x86, updateDetails);
                    CreateUpdateTask(time, "x64", outDir, mainExeName, description, x64, updateDetails);

                    File.WriteAllText(Path.Combine(outDir, "versions.json"), JsonConvert.SerializeObject(updateDetails));

                    Console.WriteLine();
                    Console.WriteLine(" --- Update package complete! --- ");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch
            {
                Console.WriteLine("Usage: create-update [inDir] [outDir] [mainExe] [description...]");
            }
        }

        private static void CreateUpdateTask(DateTime time, string platform, string outDir, string mainExeName, string description, string folder, List<UpdateDetails> updateDetails)
        {
            Console.WriteLine();
            Console.WriteLine($" --- Creating {platform} update package --- ");
            Console.WriteLine();

            var details = new UpdateDetails()
            {
                Platform = platform,
                MainExecutable = mainExeName,
                ReleaseDate = time,
                Files = new List<UpdateFile>(),
                VersionDetails = description
            };

            DoCreateUpdate(details, mainExeName, folder, outDir);

            foreach (var file in details.Files)
            {
                var lastSeen = updateDetails.LastOrDefault(d => d.Platform == platform && d.Files.Any(f => f.Name == file.Name && f.Hash.SequenceEqual(file.Hash)));
                if (lastSeen != null)
                {
                    Console.WriteLine($"Omitting \"{file.Name}\" because the same file exists in version {lastSeen.Version}...");
                    File.Delete(Path.Combine(outDir, details.Version.ToString() + "-" + details.Platform, string.Join("", file.Hash.Select(b => b.ToString("x2"))) + ".gz"));
                    file.IncludedVersion = lastSeen.Version;
                }
            }

            updateDetails.Add(details);
        }

        private static void DoCreateUpdate(UpdateDetails details, string mainExeName, string folder, string outDir)
        {
            if (!File.Exists(Path.Combine(folder, mainExeName)))
            {
                throw new Exception("Main .exe doesn't exist!");
            }

            var asm = Assembly.ReflectionOnlyLoadFrom(Path.Combine(folder, mainExeName));
            details.Version = asm.GetName().Version;

            var subDir = Path.Combine(outDir, details.Version.ToString() + "-" + details.Platform);
            Directory.CreateDirectory(subDir);

            long totalIn = 0;
            long totalOut = 0;

            Parallel.ForEach(
                Directory.GetFiles(folder, "*", SearchOption.AllDirectories),
                new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                () => SHA256.Create(),
                (f, l, h) => DoFile(details, folder, f, h, subDir, ref totalIn, ref totalOut),
                (h) => h.Dispose());

            Console.WriteLine($"Created update folder! Originally {Files.SizeSuffix(totalIn)}, now {Files.SizeSuffix(totalOut)}. Saved {Files.SizeSuffix(totalIn - totalOut)}.");
        }

        private static SHA256 DoFile(UpdateDetails details, string folder, string file, SHA256 hash, string filesDir, ref long totalIn, ref long totalOut)
        {
            var rawName = file.Remove(0, folder.Length).Trim('\\', '/');
            var outPath = "";

            Console.WriteLine($"Hashing and compressing \"{rawName}\"...");

            using (var stream = File.OpenRead(file))
            {
                var computed = hash.ComputeHash(stream);
                stream.Seek(0, SeekOrigin.Begin);

                outPath = Path.Combine(filesDir, string.Join("", computed.Select(b => b.ToString("x2"))) + ".gz");

                using (var outStream = File.Create(outPath))
                using (var compress = new GZipStream(outStream, CompressionMode.Compress))
                {
                    stream.CopyTo(compress);
                    details.Files.Add(new UpdateFile() { Name = rawName, Hash = computed });
                }
            }

            var inInfo = new FileInfo(file);
            var outInfo = new FileInfo(outPath);

            totalIn += inInfo.Length;
            totalOut += outInfo.Length;

            Console.WriteLine($"Compressed \"{rawName}\"!");

            return hash;
        }
    }
}
