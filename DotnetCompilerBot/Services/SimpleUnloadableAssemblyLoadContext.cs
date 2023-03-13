using System.Reflection;
using System.Runtime.Loader;

namespace DotnetCompilerBot.Services
{
    public class SimpleUnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        public SimpleUnloadableAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return default;
        }
    }
}
