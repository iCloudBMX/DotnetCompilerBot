namespace DotnetCompilerBot.Exceptions;

public class CompileFailedException : Exception
{
	public CompileFailedException(string message)
		: base(message) 
	{
	}
}
