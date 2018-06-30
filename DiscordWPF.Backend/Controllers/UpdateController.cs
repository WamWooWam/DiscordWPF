using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordWPF.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DiscordWPF.Backend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public UpdateController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Channels()
        {
            return new string[] { "win32-canary", "win32-stable", "win10-canary", "win10-stable" };
        }

        [HttpGet("{id}")]
        public ActionResult<UpdateDetails> Details(string id, [FromQuery]string c)
        {
            if (Version.TryParse(id, out var version))
            {
                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Data", c, "versions.json");

                var baseUri = new Uri(Path.Combine(_hostingEnvironment.ContentRootPath, "Data"));
                var dirUri = new Uri(path);

                if (System.IO.File.Exists(path) && baseUri.IsBaseOf(dirUri))
                {
                    var versions = JsonConvert.DeserializeObject<Dictionary<string, UpdateDetails>>(System.IO.File.ReadAllText(path))
                        .Select(k => new { Version = Version.Parse(k.Key), Details = k.Value })
                        .OrderBy(v => v.Version)
                        .ToDictionary(k => k.Version, v => v.Details)
                        .ToArray();

                    var newVersion = versions.FirstOrDefault(v => v.Key == version).Value;
                    if (newVersion != null)
                    {
                        return newVersion;
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }

            return BadRequest();
        }

        // GET api/values/5
        [HttpPost]
        public ActionResult<UpdateResponse> Check([FromBody] UpdateRequest request)
        {
            if (Version.TryParse(request.CurrentVersion, out var version))
            {
                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Data", request.CurrentChannel, "versions.json");

                var baseUri = new Uri(Path.Combine(_hostingEnvironment.ContentRootPath, "Data"));
                var dirUri = new Uri(path);

                if (System.IO.File.Exists(path) && baseUri.IsBaseOf(dirUri))
                {
                    var versions = JsonConvert.DeserializeObject<Dictionary<string, UpdateDetails>>(System.IO.File.ReadAllText(path))
                        .Select(k => new { Version = Version.Parse(k.Key), Details = k.Value })
                        .OrderBy(v => v.Version)
                        .ToDictionary(k => k.Version, v => v.Details)
                        .ToArray();

                    var newVersion = versions.FirstOrDefault(v => v.Key > version).Value;
                    if (newVersion != null)
                    {
                        return new UpdateResponse() { UpdateAvailable = true, Details = newVersion };
                    }
                    else
                    {
                        return new UpdateResponse() { UpdateAvailable = false };
                    }
                }
            }

            return BadRequest();
        }

        // POST api/values
        [HttpGet("{file}")]
        public IActionResult GetFile([FromRoute]string file, [FromQuery]string c, [FromQuery]string v)
        {
            // var arr = new byte[128];
            if (TryGetHash(file, out byte[] hash))
            {
                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Data", c, v);
                var verFile = Path.Combine(path, $"{v}.json");

                var baseUri = new Uri(Path.Combine(_hostingEnvironment.ContentRootPath, "Data"));
                var dirUri = new Uri(path);

                if (baseUri.IsBaseOf(dirUri) && Directory.Exists(path) && System.IO.File.Exists(verFile))
                {
                    var info = JsonConvert.DeserializeObject<UpdateDetails>(System.IO.File.ReadAllText(verFile));
                    file = info.Files.FirstOrDefault(i => i.Value.SequenceEqual(hash)).Key;
                    if (file != default)
                    {
                        var filePath = Path.Combine(path, "data", file + ".gz");

                        Uri uri = new Uri(filePath);
                        Uri dir = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Data"));

                        if (dir.IsBaseOf(uri) && System.IO.File.Exists(filePath))
                        {
                            return File(System.IO.File.OpenRead(filePath), "application/octet-stream");
                        }
                    }
                }
            }
            else
            {
                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Data", c, v);
                var verFile = Path.Combine(path, $"{v}.json");

                var baseUri = new Uri(Path.Combine(_hostingEnvironment.ContentRootPath, "Data"));
                var dirUri = new Uri(path);

                if (baseUri.IsBaseOf(dirUri) && Directory.Exists(path) && System.IO.File.Exists(verFile))
                {
                    var info = JsonConvert.DeserializeObject<UpdateDetails>(System.IO.File.ReadAllText(verFile));
                    if (info.Files.TryGetValue(file, out _))
                    {
                        var filePath = Path.Combine(path, "data", file + ".gz");

                        Uri uri = new Uri(filePath);
                        Uri dir = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Data"));

                        if (dir.IsBaseOf(uri) && System.IO.File.Exists(filePath))
                        {
                            return File(System.IO.File.OpenRead(filePath), "application/octet-stream");
                        }
                    }
                }
            }
            return NotFound();
        }

        private bool TryGetHash(string file, out byte[] hash)
        {
            int NumberChars = file.Length;
            hash = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                if (byte.TryParse(file.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    hash[i / 2] = b;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
