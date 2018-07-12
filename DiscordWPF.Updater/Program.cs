using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF.Updater
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            App.Main();
        }

        private static Dictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private static Lazy<Assembly> _currentAssembly = new Lazy<Assembly>(() => Assembly.GetExecutingAssembly());
        private static Lazy<string[]> _currentAssemblyResources = new Lazy<string[]>(() => _currentAssembly.Value.GetManifestResourceNames());

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var resourceName = $"{assemblyName.Name}.dll";

            if (_assemblyCache.TryGetValue(resourceName, out var asm))
            {
                return asm;
            }
            else
            {
                var name = _currentAssemblyResources.Value.FirstOrDefault(s => s.EndsWith(resourceName));
                if (name != null)
                {
                    using (var str = _currentAssembly.Value.GetManifestResourceStream(name))
                    {
                        var bytes = new byte[str.Length];
                        str.Read(bytes, 0, bytes.Length);

                        try
                        {
                            return Assembly.Load(bytes);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to load {resourceName}!!, Exception: {ex.Message}");
                        }
                    }
                }
            }

            return null;
        }
    }
}
