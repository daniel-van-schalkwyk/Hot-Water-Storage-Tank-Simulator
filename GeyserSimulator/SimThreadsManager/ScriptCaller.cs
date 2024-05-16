using System.Diagnostics;

namespace GeyserSimulator.SimThreadsManager;

public class ScriptCaller
{
    private readonly Process process;
    
    public ScriptCaller(string scriptExePath, string arguments)
    {
        // Create a new process to run MATLAB
        process = new Process();

        // Specify the MATLAB executable and arguments
        process.StartInfo.FileName = scriptExePath;
        process.StartInfo.Arguments = arguments;

        // Redirect MATLAB's stdout/stderr
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        // Event handlers for output
        process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
        // process.ErrorDataReceived += (sender, e) => Console.WriteLine($"Error: {e.Data}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool CallScript()
    {
        // Start the MATLAB process
        Console.WriteLine($"Script executable process started at {DateTime.Now}");
        process.Start();

        // Begin redirecting output
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for MATLAB process to exit
        process.WaitForExit();

        // Close the process
        process.Close();

        Console.WriteLine($"Script executable process finished at {DateTime.Now}");
        return true;
    }
}