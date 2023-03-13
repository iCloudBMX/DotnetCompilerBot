using DotnetCompilerBot.Exceptions;
using DotnetCompilerBot.Extensions;
using DotnetCompilerBot.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotnetCompilerBot.Services;

public class CompilerService : ICompilerService
{
    #region Compile process

    public byte[] Compile(string sourceCode)
    {
        using var peStream = new MemoryStream();

        var result = GenerateCode(sourceCode).Emit(peStream);

        if (result.Success is false)
        {
            StringBuilder message = new StringBuilder();
            
            message.AppendLine(MessageTemplate.GetDecoratedMessage(
                message: "Compilation done with errors:",
                decoraterType: DecoraterType.Bold));

            var failures = result
                .Diagnostics
                .Where(diagnostic => diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (var diagnostic in failures)
            {
                string diagnosticMessage = diagnostic
                    .GetMessage()
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

                string diagnosticId = diagnostic.Id;

                message.AppendLine(MessageTemplate.GetDecoratedMessage(
                    message: $"{diagnosticId}: {diagnosticMessage}",
                    decoraterType: DecoraterType.Monospace));

                message.AppendLine();
            }

            throw new CompileFailedException(message.ToString());
        }

        peStream.Seek(0, SeekOrigin.Begin);

        return peStream.ToArray();
    }

    private static CSharpCompilation GenerateCode(string sourceCode)
    {
        var codeString = SourceText.From(sourceCode);
        
        var options = CSharpParseOptions
            .Default
            .WithLanguageVersion(LanguageVersion.CSharp11);

        var parsedSyntaxTree = SyntaxFactory
            .ParseSyntaxTree(codeString, options);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };

        Assembly
            .GetEntryAssembly()?
            .GetReferencedAssemblies()
            .ToList()
            .ForEach(assembly => references
                .Add(MetadataReference.CreateFromFile(Assembly.Load(assembly).Location)));

        return CSharpCompilation.Create("HelloWorld.dll",
            new[] { parsedSyntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
    }

    #endregion

    #region Run process
    public string Execute(byte[] compiledAssembly)
    {
        var assemblyLoadContextWeakRef = LoadAndExecute(compiledAssembly, out string output);

        for (var i = 0; i < 8 && assemblyLoadContextWeakRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return output;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndExecute(byte[] compiledAssembly, out string output)
    {
        using (var memoryStream = new MemoryStream(compiledAssembly))
        {
            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
            var assembly = assemblyLoadContext.LoadFromStream(memoryStream);
            var entry = assembly.EntryPoint;

            using var outputStream = new MemoryStream();
            using var streamWriter = new StreamWriter(outputStream);
            streamWriter.AutoFlush = true;

            var originalConsoleOut = Console.Out;
            Console.SetOut(streamWriter);

            if(entry is not null && entry.GetParameters().Length > 0)
            {
                entry.Invoke(null, new object[] { Array.Empty<string>() });
            }
            else
            {
                entry.Invoke(null, null);
            }

            Console.SetOut(originalConsoleOut);
            output = Encoding.UTF8.GetString(outputStream.ToArray());
            assemblyLoadContext.Unload();

            return new WeakReference(assemblyLoadContext);
        }
    }
    #endregion
}