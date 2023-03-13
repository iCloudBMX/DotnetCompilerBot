namespace DotnetCompilerBot.Services;

public interface ICompilerService
{
    byte[] Compile(string sourceCode);
    string Execute(byte[] compiledAssembly);
}