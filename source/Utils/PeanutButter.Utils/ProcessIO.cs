using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

// ReSharper disable InconsistentNaming

#if BUILD_PEANUTBUTTER_INTERNAL
namespace Imported.PeanutButter.Utils
#else
namespace PeanutButter.Utils
#endif
{
    /// <summary>
    /// Wraps process IO (stdout, stderr) into an easy-to-access disposable source
    /// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public
#endif
        interface IProcessIO : IDisposable
    {
        /// <summary>
        /// The process id
        /// </summary>
        int ProcessId { get; }

        /// <summary>
        /// True if the process started properly
        /// </summary>
        bool Started { get; }

        /// <summary>
        /// Set if the process didn't start properly, to the exception thrown
        /// </summary>
        Exception StartException { get; }

        /// <summary>
        /// Read lines from stdout until the process exits
        /// </summary>
        IEnumerable<string> StandardOutput { get; }

        /// <summary>
        /// Read lines from stderr until the process exits
        /// </summary>
        IEnumerable<string> StandardError { get; }

        /// <summary>
        /// Read the lines from stderr and stdout (until the process exits),
        /// interleaved (mostly in order, though some minor out-of-order
        /// situations can occur between stderr and stdout if there is
        /// rapid output on both because of the async io handlers for
        /// dotnet Process objects
        /// </summary>
        IEnumerable<string> StandardOutputAndErrorInterleaved { get; }

        /// <summary>
        /// Read the lines captured thus far from stdout -
        /// does not wait for the process to complete
        /// </summary>
        IEnumerable<string> StandardOutputSnapshot { get; }

        /// <summary>
        /// Read the lines captured thus far from stderr -
        /// does not wait for the process to complete
        /// </summary>
        IEnumerable<string> StandardErrorSnapshot { get; }

        /// <summary>
        /// Read the lines captured thus far from stderr and stdout,
        /// interleaved (mostly in order, though some minor out-of-order
        /// situations can occur between stderr and stdout if there is
        /// rapid output on both because of the async io handlers for
        /// dotnet Process objects
        /// - does not wait for the process to complete
        /// </summary>
        IEnumerable<string> StandardOutputAndErrorInterleavedSnapshot { get; }

        /// <summary>
        /// stdin for the process
        /// </summary>
        StreamWriter StandardInput { get; }

        /// <summary>
        /// Access to the underlying Process
        /// </summary>
        Process Process { get; }

        /// <summary>
        /// Provides access to the exit code of the process,
        /// waiting for it to complete if necessary
        /// </summary>
        int ExitCode { get; }

        /// <summary>
        /// Flag: true when the process has exited (or
        /// couldn't start up)
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// The program started by this ProcessIO
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// A copy of the commandline arguments to that program
        /// </summary>
        string[] Arguments { get; }

        /// <summary>
        /// The working directory in which the process was launched
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Renders the commandline used to start this process
        /// </summary>
        string Commandline { get; }

        /// <summary>
        /// Wait for the process to exit and return the exit code
        /// </summary>
        /// <returns></returns>
        int WaitForExit();

        /// <summary>
        /// Wait up to the timeout for the process to exit
        /// and return the exit code, if available
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        int? WaitForExit(int timeoutMilliseconds);

        /// <summary>
        /// Kill the underlying process
        /// </summary>
        void Kill();

        /// <summary>
        /// Waits for some output to be emitted from the process
        /// </summary>
        /// <param name="io"></param>
        /// <param name="matcher"></param>
        bool WaitForOutput(
            StandardIo io,
            Func<string, bool> matcher
        );

        /// <summary>
        /// Waits for some output to be emitted from the process
        /// </summary>
        /// <param name="io"></param>
        /// <param name="matcher"></param>
        /// <param name="timeoutMilliseconds"></param>
        bool WaitForOutput(
            StandardIo io,
            Func<string, bool> matcher,
            int timeoutMilliseconds
        );
    }

    /// <summary>
    /// 
    /// </summary>
    public enum StandardIo
    {
        /// <summary>
        /// 
        /// </summary>
        StdOut,

        /// <summary>
        /// 
        /// </summary>
        StdErr,

        /// <summary>
        /// 
        /// </summary>
        StdOutOrStdErr
    }

    /// <summary>
    /// Provides the contract for an unstarted ProcessIO, as would be
    /// obtained from `ProcessIO.In(workingDir)`
    /// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public
#endif
        interface IUnstartedProcessIO : IProcessIO
    {
        /// <summary>
        /// Starts the process in the previously provided working directory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        IProcessIO Start(string filename, params string[] arguments);

        /// <summary>
        /// Adds another environment variable to the process startup environment
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IUnstartedProcessIO WithEnvironmentVariable(string name, string value);

        /// <summary>
        /// Set the working directory for the process; use if you started
        /// the fluent chain with `WithEnvironmentVariable`
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        IUnstartedProcessIO In(string workingDirectory);

        /// <summary>
        /// Establish a set of environment variables for the process
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        IUnstartedProcessIO WithEnvironment(IDictionary<string, string> env);
    }

    /// <inheritdoc />
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public
#endif
        class ProcessIO : IProcessIO
    {
        /// <inheritdoc />
        public string Filename { get; private set; }

        /// <inheritdoc />
        public string[] Arguments { get; private set; }

        /// <inheritdoc />
        public string WorkingDirectory { get; private set; }

        /// <inheritdoc />
        public string Commandline =>
            _commandline ??= RenderCommandline();

        private string _commandline;

        /// <inheritdoc />
        public int ProcessId
        {
            get
            {
                try
                {
                    return _process?.Id ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <inheritdoc />
        public bool Started { get; private set; }

        /// <inheritdoc />
        public Exception StartException { get; private set; }

        /// <inheritdoc />
        public Process Process => _process;

        private Process _process;
        private bool _disposed;
        private ManualResetEventSlim _stdOutDataAvailable;
        private ManualResetEventSlim _stdErrDataAvailable;
        private ManualResetEventSlim _interleavedDataAvailable;


        /// <summary>
        /// Run the provided command, pipe output as it streams
        /// </summary>
        /// <param name="filename">app to run</param>
        /// <param name="arguments">args for that app</param>
        [Obsolete(
            "Please use the static Process.Start or Process.In helpers; this constructor will be made internal in the future"
        )]
        public ProcessIO(
            string filename,
            params string[] arguments
        )
        {
            StartInFolder(Environment.CurrentDirectory, filename, arguments, null);
        }

        private ProcessIO()
        {
        }

        private string RenderCommandline()
        {
            return $"{QuoteIfNecessary(Filename)} {Arguments.Select(QuoteIfNecessary).JoinWith(" ")}";
        }

        /// <summary>
        /// Represents an unstarted process-io instance
        /// </summary>
        public class UnstartedProcessIO : ProcessIO, IUnstartedProcessIO
        {
            private readonly Dictionary<string, string> _environment = new Dictionary<string, string>();

            /// <inheritdoc />
            internal UnstartedProcessIO(string workingDirectory)
            {
                SetWorkingDirectory(workingDirectory);
            }

            private void SetWorkingDirectory(string workingDirectory)
            {
                if (string.IsNullOrWhiteSpace(workingDirectory))
                {
                    throw new ArgumentException(
                        $"{nameof(workingDirectory)} must be provided",
                        nameof(workingDirectory)
                    );
                }

                if (!Directory.Exists(workingDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(workingDirectory);
                    }
                    catch
                    {
                        throw new ArgumentException(
                            $"Unable to find or create working directory '{workingDirectory}'",
                            nameof(workingDirectory)
                        );
                    }
                }

                WorkingDirectory = workingDirectory;
            }


            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new IProcessIO Start(string filename, params string[] arguments)
            {
                return StartInFolder(
                    WorkingDirectory,
                    filename,
                    arguments,
                    _environment
                );
            }

            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new IUnstartedProcessIO WithEnvironmentVariable(
                string name,
                string value
            )
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException($"environment variable name may not be null or blank");
                }

                _environment[name] = value;
                return this;
            }

            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new IUnstartedProcessIO In(string workingDirectory)
            {
                SetWorkingDirectory(workingDirectory);
                return this;
            }

            /// <inheritdoc />
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new IUnstartedProcessIO WithEnvironment(IDictionary<string, string> env)
            {
                return env?.Aggregate(
                    this as IUnstartedProcessIO,
                    (acc, cur) => acc.WithEnvironmentVariable(cur.Key, cur.Value)
                ) ?? this;
            }
        }

        /// <summary>
        /// Sets up ProcessIO to run within the provided folder. Usage:
        /// using var io = ProcessIO.In("/path/to/folder").Start("cmd", "arg1", "arg2")
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        public static UnstartedProcessIO In(string workingDirectory)
        {
            return new UnstartedProcessIO(workingDirectory);
        }

        /// <summary>
        /// Starts a ProcessIO instance for the given filename and args in the current
        /// working directory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IProcessIO Start(string filename, params string[] args)
        {
#pragma warning disable 618
            return new ProcessIO(filename, args);
#pragma warning restore 618
        }

        private ProcessIO StartInFolder(
            string workingDirectory,
            string filename,
            string[] arguments,
            IDictionary<string, string> environment
        )
        {
            if (Started && !(_process?.HasExited ?? true))
            {
                throw new InvalidOperationException($"Process already started: {_process.Id}");
            }

            var processEnvironment = GenerateProcessEnvironmentFor(environment);

            try
            {
                _process = new Process()
                {
                    StartInfo =
                    {
                        FileName = filename,
                        Arguments = MakeArgsFrom(arguments),
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = workingDirectory,
                    }
                };
                processEnvironment.ForEach(
                    kvp =>
                    {
                        _process.StartInfo.Environment[kvp.Key] = kvp.Value;
                    }
                );
                _stdOutDataAvailable = new();
                _stdErrDataAvailable = new();
                _interleavedDataAvailable = new();

                _process.Exited += OnProcessExit;
                _process.OutputDataReceived += OnOutputReceived;
                _process.ErrorDataReceived += OnErrReceived;
                _process.Start();
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
                Arguments = arguments;
                Filename = filename;
                Started = true;
            }
            catch (Exception ex)
            {
                StartException = ex;
                _process = null;
            }

            return this;
        }

        private void OnErrReceived(object sender, DataReceivedEventArgs e)
        {
            lock (_ioLock)
            {
                var data = e.Data;
                if (data is null)
                {
                    _stdErrDataAvailable.Set();
                    return;
                }

                _stdErrBuffer.Enqueue(data);
                _stdErrDataAvailable.Set();
                _interleavedBuffer.Enqueue(data);
                _interleavedDataAvailable.Set();
            }
        }

        private readonly ConcurrentQueue<string> _interleavedBuffer = new();
        private readonly ConcurrentQueue<string> _stdOutBuffer = new();
        private readonly ConcurrentQueue<string> _stdErrBuffer = new();

        private readonly object _ioLock = new();

        private void OnOutputReceived(
            object sender,
            DataReceivedEventArgs e
        )
        {
            lock (_ioLock)
            {
                var data = e.Data;
                if (data is null)
                {
                    _stdOutDataAvailable.Set();
                    return;
                }

                _stdOutBuffer.Enqueue(data);
                _stdOutDataAvailable.Set();
                _interleavedBuffer.Enqueue(data);
                _interleavedDataAvailable.Set();
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            _stdErrDataAvailable.Set();
            _stdOutDataAvailable.Set();
            _interleavedDataAvailable.Set();
        }

        private static IDictionary<string, string> GenerateProcessEnvironmentFor(
            IDictionary<string, string> environment
        )
        {
            var processEnvironment = Environment.GetEnvironmentVariables()
                .ToDictionary<string, string>();
            environment?.ForEach(
                kvp =>
                {
                    if (kvp.Value is null)
                    {
                        if (processEnvironment.ContainsKey(kvp.Key))
                        {
                            processEnvironment.Remove(kvp.Key);
                        }
                    }
                    else
                    {
                        processEnvironment[kvp.Key] = kvp.Value;
                    }
                }
            );
            return processEnvironment;
        }

        /// <inheritdoc />
        public int ExitCode
        {
            get
            {
                if (_process is null)
                {
                    throw new InvalidOperationException(
                        "Process has not started yet"
                    );
                }

                if (!_process.HasExited)
                {
                    _process.WaitForExit();
                }

                return _process.ExitCode;
            }
        }

        /// <inheritdoc />
        public int WaitForExit()
        {
            return ExitCode;
        }

        /// <inheritdoc />
        public int? WaitForExit(int timeoutMs)
        {
            if (_process is null)
            {
                throw new InvalidOperationException(
                    "Process has not started yet"
                );
            }

            if (_process.HasExited)
            {
                return _process.ExitCode;
            }

            return _process.WaitForExit(timeoutMs)
                ? _process.ExitCode
                : null;
        }

        /// <inheritdoc />
        public void Kill()
        {
            if (HasExited)
            {
                return;
            }

            _process?.Kill();
        }

        /// <inheritdoc />
        public bool WaitForOutput(
            StandardIo io,
            Func<string, bool> matcher
        )
        {
            return WaitForOutput(
                io,
                matcher,
                int.MaxValue
            );
        }

        /// <inheritdoc />
        public bool WaitForOutput(
            StandardIo io,
            Func<string, bool> matcher,
            int timeoutMilliseconds
        )
        {
            switch (io)
            {
                case StandardIo.StdErr:
                    return WaitForOutput(
                        StandardErrorSnapshot,
                        _stdErrDataAvailable,
                        matcher,
                        timeoutMilliseconds
                    );
                case StandardIo.StdOut:
                    return WaitForOutput(
                        StandardOutputSnapshot,
                        _stdOutDataAvailable,
                        matcher,
                        timeoutMilliseconds
                    );
                case StandardIo.StdOutOrStdErr:
                    return WaitForOutput(
                        StandardOutputAndErrorInterleavedSnapshot,
                        _interleavedDataAvailable,
                        matcher,
                        timeoutMilliseconds
                    );
                default:
                    throw new ArgumentException(
                        $"StandardIo value {io} is not handled"
                    );
            }
        }

        private bool WaitForOutput(
            IEnumerable<string> snapshotSource,
            ManualResetEventSlim ev,
            Func<string, bool> matcher,
            int timeoutMilliseconds
        )
        {
            var offset = 0;
            // ReSharper disable once PossibleMultipleEnumeration
            if (HaveOutput(snapshotSource, matcher, ref offset))
            {
                return true;
            }

            var maxLoopTimeout = 100;
            var timeout = Math.Min(maxLoopTimeout, timeoutMilliseconds);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!HasExited && timeout > 0)
            {
                if (ev.Wait(timeout))
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    if (HaveOutput(snapshotSource, matcher, ref offset))
                    {
                        return true;
                    }
                }

                timeout = Math.Min(
                    maxLoopTimeout,
                    timeoutMilliseconds - (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds)
                );
            }

            if (timeout < 1)
            {
                return false;
            }

            // ReSharper disable once PossibleMultipleEnumeration
            return HaveOutput(snapshotSource, matcher, ref offset);
        }

        private bool HaveOutput(
            IEnumerable<string> snapshotSource,
            Func<string, bool> matcher,
            ref int offset
        )
        {
            var snapshot = snapshotSource.Skip(offset).ToArray();
            return snapshot.Any(matcher);
        }

        /// <inheritdoc />
        public IEnumerable<string> StandardOutput =>
            Enumerate(_stdOutBuffer, _stdOutDataAvailable);

        /// <inheritdoc />
        public IEnumerable<string> StandardOutputSnapshot =>
            EnumerateSnapshot(_stdOutBuffer);

        /// <inheritdoc />
        public IEnumerable<string> StandardError =>
            Enumerate(_stdErrBuffer, _stdErrDataAvailable);

        /// <inheritdoc />
        public IEnumerable<string> StandardErrorSnapshot =>
            EnumerateSnapshot(_stdErrBuffer);


        /// <inheritdoc />
        public IEnumerable<string> StandardOutputAndErrorInterleaved
            => Enumerate(_interleavedBuffer, _interleavedDataAvailable);

        /// <inheritdoc />
        public IEnumerable<string> StandardOutputAndErrorInterleavedSnapshot
            => EnumerateSnapshot(_interleavedBuffer);

        private IEnumerable<string> EnumerateSnapshot(
            ConcurrentQueue<string> data
        )
        {
            foreach (var line in data)
            {
                yield return line;
            }
        }


        private IEnumerable<string> Enumerate(
            ConcurrentQueue<string> data,
            ManualResetEventSlim available
        )
        {
            var lineCount = 0;
            foreach (var line in ReadFromOffset(data, 0))
            {
                lineCount++;
                yield return line;
            }

            while (true)
            {
                if (available is null)
                {
                    yield break;
                }

                while (!HasExited)
                {
                    if (available.Wait(1000))
                    {
                        break;
                    }
                }

                available.Reset();

                foreach (var line in ReadFromOffset(data, lineCount))
                {
                    lineCount++;
                    yield return line;
                }

                if (HasExited)
                {
                    // drain anything left and break out
                    foreach (var line in ReadFromOffset(data, lineCount))
                    {
                        lineCount++;
                        yield return line;
                    }

                    yield break;
                }
            }
        }

        private IEnumerable<string> ReadFromOffset(
            IEnumerable<string> source,
            int offset
        )
        {
            var snapshot = source.Skip(offset).ToArray();
            foreach (var line in snapshot)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Direct access to the StandardInput on the process
        /// </summary>
        public StreamWriter StandardInput =>
            SafeProcessAccess.StandardInput;

        private Process SafeProcessAccess
        {
            get
            {
                var p = _process;
                if (p is null)
                {
                    throw new Exception("Process has not started");
                }

                if (p.HasExited)
                {
                    throw new Exception("Process has already exited");
                }

                return p;
            }
        }

        /// <inheritdoc />
        public bool HasExited
        {
            get
            {
                try
                {
                    return _disposed ||
                        (_process?.HasExited ?? StartException is not null);
                }
                catch
                {
                    // access into _process failed: process
                    // is probably starting up
                    return false;
                }
            }
        }

        private string MakeArgsFrom(string[] parameters)
        {
            return string.Join(
                " ",
                parameters
                    .Select(QuoteIfNecessary)
            );
        }

        /// <summary>
        /// Quotes a string if it's necessary:
        /// - contains whitespace
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string QuoteIfNecessary(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "\"\"";
            }

            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                return str;
            }

            return AnyWhitespace.Match(str).Success
                ? $"\"{str}\""
                : str;
        }

        private static readonly Regex AnyWhitespace = new Regex(
            "\\s+",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Kills the process if it hasn't finished yet
        /// - you should always dispose, since you may decide not to read until the process is dead
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            if (!_process?.HasExited ?? false)
            {
                try
                {
                    _process?.Kill();
                }
                catch
                {
                    /* intentionally suppressed */
                }

                _process?.Dispose();
            }

            _process = null;
        }

        /// <summary>
        /// Sets up for the new process to use the provided environment variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static IUnstartedProcessIO WithEnvironmentVariable(
            string name,
            string value
        )
        {
            var result = new UnstartedProcessIO(Environment.CurrentDirectory);
            return result.WithEnvironmentVariable(name, value);
        }

        /// <summary>
        /// Sets up a bunch of environment variables for the new process
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static IUnstartedProcessIO WithEnvironment(
            IDictionary<string, string> environment
        )
        {
            var result = new UnstartedProcessIO(Environment.CurrentDirectory);
            return result.WithEnvironment(environment);
        }
    }
}