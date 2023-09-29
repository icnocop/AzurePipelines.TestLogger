using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AzurePipelines.TestLogger.Tests
{
    internal class ProcessRunner
    {
        private StringBuilder _outputAndError;

        public int Run(
            string fileName,
            List<string> arguments,
            IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            _outputAndError?.Clear();
            _outputAndError = new StringBuilder();

            Console.WriteLine($"\"{fileName}\" {string.Join(" ", arguments)}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Set environment variables
            foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
            {
                if (environmentVariable.Value != null)
                {
                    startInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;
                }
            }

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            StringBuilder outputAndError = new StringBuilder();

            // Start the process
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                Thread stdOutReaderThread = null;
                Thread stdErrReaderThread = null;

                // Invoke stdOut and stdErr readers - each
                // has its own thread to guarantee that they aren't
                // blocked by, or cause a block to, the actual
                // process running (or the gui).
                stdOutReaderThread = new Thread(this.ReadStdOut);
                stdOutReaderThread.Start(process);
                stdErrReaderThread = new Thread(this.ReadStdErr);
                stdErrReaderThread.Start(process);

                process.WaitForExit();
                int exitCode = process.ExitCode;

                if (stdOutReaderThread != null)
                {
                    // wait for thread
                    stdOutReaderThread.Join();
                }

                if (stdErrReaderThread != null)
                {
                    // wait for thread
                    stdErrReaderThread.Join();
                }

                Console.WriteLine($"Output:\n{_outputAndError.ToString()}");

                // Check the exit code
                Console.WriteLine($"Exit Code: {exitCode}");

                return exitCode;
            }
        }

        private void ReadStdOut(object processObj)
        {
            try
            {
                string str;
                while ((str = ((Process)processObj).StandardOutput.ReadLine()) != null)
                {
                    _outputAndError.AppendLine(str);
                }
            }
            catch
            {
            }
        }

        private void ReadStdErr(object processObj)
        {
            try
            {
                string str;
                while ((str = ((Process)processObj).StandardError.ReadLine()) != null)
                {
                    _outputAndError.AppendLine(str);
                }
            }
            catch
            {
            }
        }
    }
}
