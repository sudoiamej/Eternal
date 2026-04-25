using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace Eternal.Services.System
{
    public class WindowsIntelligenceEngine : INeuralAdvisorService, IDisposable
    {
        private Model? _model;
        private Tokenizer? _tokenizer;
        private readonly string _modelPath;
        private readonly ILoggingService _loggingService;

        public bool IsModelAvailable => Directory.Exists(_modelPath) && File.Exists(Path.Combine(_modelPath, "model.onnx"));
        public bool IsHardwareCompatible { get; private set; }
        public string CompatibilityMessage { get; private set; } = "Initializing...";

        public WindowsIntelligenceEngine(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _modelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eternal", "Neuralis", "phi3-mini");
            
            CheckHardwareCompatibility();
        }

        private void CheckHardwareCompatibility()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    ulong totalBytes = (ulong)obj["TotalPhysicalMemory"];
                    double totalGB = totalBytes / 1024.0 / 1024.0 / 1024.0;

                    if (totalGB < 3.5) // Allow margin for 4GB systems
                    {
                        IsHardwareCompatible = false;
                        CompatibilityMessage = $"Insufficient RAM: {totalGB:F1}GB detected (4GB required for local AI).";
                    }
                    else if (totalGB < 7.5)
                    {
                        IsHardwareCompatible = true;
                        CompatibilityMessage = "System meets minimum requirements (4GB), but performance may be limited.";
                    }
                    else
                    {
                        IsHardwareCompatible = true;
                        CompatibilityMessage = "System meets requirements for local neural inference.";
                    }
                }
            }
            catch (Exception ex)
            {
                IsHardwareCompatible = false;
                CompatibilityMessage = $"Safety Check Error: {ex.Message}";
            }
        }

        private async Task EnsureModelLoadedAsync()
        {
            if (_model != null) return;

            if (!IsHardwareCompatible)
                throw new InvalidOperationException("Local AI is disabled due to hardware constraints.");

            if (!IsModelAvailable)
                throw new FileNotFoundException($"Neural model not found at {_modelPath}.");

            await Task.Run(() =>
            {
                _loggingService.Log("Neuralis: Loading 2.3GB Phi-3 model into RAM...");
                _model = new Model(_modelPath);
                _tokenizer = new Tokenizer(_model);
                _loggingService.Log("Neuralis: Model initialized successfully.");
            });
        }

        public async IAsyncEnumerable<string> AskAdvisorAsync(string systemContext, string userQuery, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await EnsureModelLoadedAsync();

            string prompt = $"<|system|>\nYou are a Senior Windows Systems Engineer and Malware Analyst. Analyze the system telemetry provided and solve the user's problem. Be concise and technical.\nContext:\n{systemContext}\n<|user|>\n{userQuery}\n<|assistant|>\n";

            using var tokens = _tokenizer!.Encode(prompt);
            using var generatorParams = new GeneratorParams(_model!);
            generatorParams.SetSearchOption("max_length", 1024);
            generatorParams.SetSearchOption("past_present_share_buffer", false);

            using var generator = new Generator(_model!, generatorParams);
            generator.AppendTokenSequences(tokens); // Replaces SetInputSequences in 0.6.0+

            while (!generator.IsDone())
            {
                if (cancellationToken.IsCancellationRequested) break;

                yield return await Task.Run(() =>
                {
                    // ComputeLogits() was REMOVED in 0.6.0. GenerateNextToken handles it.
                    generator.GenerateNextToken();
                    var sequence = generator.GetSequence(0);
                    if (sequence.Length == 0) return string.Empty;
                    return _tokenizer.Decode(new int[] { sequence[sequence.Length - 1] });
                });
            }
        }

        public void UnloadModel()
        {
            _loggingService.Log("Neuralis: Disposing model to free system resources.");
            _model?.Dispose();
            _tokenizer?.Dispose();
            _model = null;
            _tokenizer = null;
            GC.Collect();
        }

        public void Dispose() => UnloadModel();

        IAsyncEnumerable<string> INeuralAdvisorService.AskAdvisorAsync(string systemContext, string userQuery)
        {
            return AskAdvisorAsync(systemContext, userQuery);
        }
    }
}
