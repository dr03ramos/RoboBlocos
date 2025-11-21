using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RoboBlocos.Models;

namespace RoboBlocos.Services
{
    /// <summary>
    /// Serviço responsável por compilar e enviar código NQC para o robô
    /// </summary>
    public class NqcCompilerService
    {
        private readonly string _nqcExePath;

        /// <summary>
        /// Construtor padrão que inicializa o caminho do executável NQC
        /// </summary>
        public NqcCompilerService()
        {
            _nqcExePath = GetNqcExecutablePath();
        }

        /// <summary>
        /// Compila e envia o código NQC para o robô (fluxo completo)
        /// </summary>
        /// <param name="nqcCode">Código NQC a ser compilado e enviado</param>
        /// <param name="settings">Configurações do projeto com informações de conexão</param>
        /// <param name="progressCallback">Callback para reportar progresso da operação</param>
        /// <param name="cancellationToken">Token para cancelamento da operação assíncrona</param>
        /// <returns>Resultado da compilação e envio</returns>
        public async Task<CompilationResult> CompileAndDownloadAsync(
            string nqcCode,
            ProjectSettings settings,
            IProgress<CompilationProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateInput(nqcCode);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            progressCallback?.Report(new CompilationProgress
            {
                Stage = CompilationStage.Compiling,
                Message = "Compilando programa...",
                CurrentAttempt = 0,
                TotalAttempts = settings.ConnectionSettings.ConnectionAttempts
            });

            var compilationResult = await ExecuteNqcWithTempFileAsync(nqcCode, settings, BuildCompileOnlyArguments, cancellationToken);
            if (compilationResult.Success)
            {
                progressCallback?.Report(new CompilationProgress
                {
                    Stage = CompilationStage.Success,
                    Message = "Compilação bem sucedida."
                });
            }
            else
            { 
                progressCallback?.Report(new CompilationProgress
                {
                    Stage = CompilationStage.Failed,
                    Message = $"Erro de compilação: {compilationResult.Message}"
                });
                return compilationResult;
            }

            int maxAttempts = Math.Max(1, settings.ConnectionSettings.ConnectionAttempts);
            CompilationResult? lastResult = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressCallback?.Report(new CompilationProgress
                {
                    Stage = CompilationStage.Downloading,
                    Message = $"Tentativa {attempt} de {maxAttempts}: Enviando para o robô {settings.RobotSettings.Model} na porta {settings.ConnectionSettings.SerialPort}...",
                    CurrentAttempt = attempt,
                    TotalAttempts = maxAttempts
                });

                lastResult = await ExecuteNqcWithTempFileAsync(nqcCode, settings, BuildDownloadArguments, cancellationToken);

                if (lastResult.Success)
                {
                    progressCallback?.Report(new CompilationProgress
                    {
                        Stage = CompilationStage.Success,
                        Message = "Programa enviado e executando no robô!",
                        CurrentAttempt = attempt,
                        TotalAttempts = maxAttempts
                    });
                    break;
                }

                if (!IsConnectionError(lastResult))
                {
                    progressCallback?.Report(new CompilationProgress
                    {
                        Stage = CompilationStage.Failed,
                        Message = lastResult.Message,
                        CurrentAttempt = attempt,
                        TotalAttempts = maxAttempts
                    });
                }

                if (attempt < maxAttempts)
                {
                    await Task.Delay(2000, cancellationToken);
                }
            }

            progressCallback?.Report(new CompilationProgress
            {
                Stage = CompilationStage.Failed,
                Message = "Não foi possível conectar ao robô após todas as tentativas.",
                CurrentAttempt = maxAttempts,
                TotalAttempts = maxAttempts
            });

            return lastResult ?? new CompilationResult
            {
                Success = false,
                Message = "Falha na conexão com o robô",
                ErrorDetails = "Não foi possível conectar ao robô. Verifique se ele está ligado e próximo ao sensor.",
                ExitCode = -1
            };
        }

        /// <summary>
        /// Valida a entrada antes de processar (código não vazio e executável existente)
        /// </summary>
        /// <param name="nqcCode">Código NQC a ser validado</param>
        /// <returns>Resultado da validação</returns>
        private CompilationResult ValidateInput(string nqcCode)
        {
            if (string.IsNullOrWhiteSpace(nqcCode))
            {
                return new CompilationResult
                {
                    Success = false,
                    Message = "Código vazio",
                    ErrorDetails = "Não há código para compilar. Adicione blocos ao seu programa.",
                    ExitCode = -1
                };
            }

            if (!File.Exists(_nqcExePath))
            {
                return new CompilationResult
                {
                    Success = false,
                    Message = "Compilador não encontrado",
                    ErrorDetails = $"O executável nqc.exe não foi encontrado em: {_nqcExePath}",
                    ExitCode = -1
                };
            }

            return new CompilationResult { Success = true };
        }

        /// <summary>
        /// Executa o NQC usando um arquivo temporário (cria, executa e deleta automaticamente)
        /// </summary>
        /// <param name="nqcCode">Código NQC a ser escrito no arquivo temporário</param>
        /// <param name="settings">Configurações do projeto</param>
        /// <param name="buildArgumentsFunc">Função para construir os argumentos do NQC</param>
        /// <param name="cancellationToken">Token para cancelamento</param>
        /// <returns>Resultado da execução</returns>
        private async Task<CompilationResult> ExecuteNqcWithTempFileAsync(
            string nqcCode,
            ProjectSettings settings,
            Func<string, ProjectSettings, string> buildArgumentsFunc,
            CancellationToken cancellationToken)
        {
            using var tempFile = new TempFile(nqcCode);
            string arguments = buildArgumentsFunc(tempFile.Path, settings);
            return await ExecuteNqcProcessAsync(arguments, cancellationToken);
        }

        /// <summary>
        /// Executa o processo NQC e captura a saída/erro
        /// </summary>
        /// <param name="arguments">Argumentos para o executável NQC</param>
        /// <param name="cancellationToken">Token para cancelamento</param>
        /// <returns>Resultado da execução do processo</returns>
        private async Task<CompilationResult> ExecuteNqcProcessAsync(string arguments, CancellationToken cancellationToken)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _nqcExePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_nqcExePath)
                };

                using var process = new Process { StartInfo = processStartInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeoutTask = Task.Delay(30000, cancellationToken);
                var processTask = Task.Run(() => process.WaitForExit(), cancellationToken);

                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    process.Kill();
                    Debug.WriteLine("[NQC] Timeout na execução do NQC.");
                    return new CompilationResult
                    {
                        Success = false,
                        Message = "Timeout na operação",
                        ErrorDetails = "A operação excedeu o tempo limite de 30 segundos.",
                        ExitCode = -1
                    };
                }

                int exitCode = process.ExitCode;
                string output = outputBuilder.ToString().Trim();
                string error = errorBuilder.ToString().Trim();

                if (exitCode == 0)
                {
                    return new CompilationResult
                    {
                        Success = true,
                        Message = "Operação concluída com sucesso",
                        ExitCode = exitCode,
                        Output = output
                    };
                }
                else
                {
                    string errorMessage = string.IsNullOrEmpty(error) ? output : error;
                    string userMessage = ParseNqcError(errorMessage, exitCode);
                    Debug.WriteLine($"[NQC] Erro na execução: {errorMessage}");
                    return new CompilationResult
                    {
                        Success = false,
                        Message = userMessage,
                        ErrorDetails = errorMessage,
                        ExitCode = exitCode,
                        Output = output
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new CompilationResult { Success = false, Message = "Operação cancelada.", ExitCode = -1 };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NQC] Exceção ao executar NQC: {ex.Message}");
                return new CompilationResult
                {
                    Success = false,
                    Message = "Erro ao executar compilador",
                    ErrorDetails = ex.Message,
                    ExitCode = -1
                };
            }
        }

        /// <summary>
        /// Constrói argumentos apenas para compilação (verifica sintaxe)
        /// </summary>
        /// <param name="sourceFilePath">Caminho do arquivo fonte</param>
        /// <param name="settings">Configurações do projeto</param>
        /// <returns>Argumentos construídos</returns>
        private string BuildCompileOnlyArguments(string sourceFilePath, ProjectSettings settings)
        {
            return $"-T{settings.RobotSettings.Model.ToUpperInvariant()} \"{sourceFilePath}\"";
        }

        /// <summary>
        /// Constrói argumentos para compilar, enviar e executar no robô
        /// </summary>
        /// <param name="sourceFilePath">Caminho do arquivo fonte</param>
        /// <param name="settings">Configurações do projeto</param>
        /// <returns>Argumentos construídos</returns>
        private string BuildDownloadArguments(string sourceFilePath, ProjectSettings settings)
        {
            return $"-T{settings.RobotSettings.Model.ToUpperInvariant()} " +
                   $"-S{settings.ConnectionSettings.SerialPort} " +
                   $"-d -pgm 1 -run \"{sourceFilePath}\"";
        }

        /// <summary>
        /// Obtém o caminho completo para o executável NQC
        /// </summary>
        /// <returns>Caminho do executável</returns>
        private static string GetNqcExecutablePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "nqc.exe");
        }

        /// <summary>
        /// Analisa o erro do NQC e retorna uma mensagem amigável
        /// </summary>
        /// <param name="errorMessage">Mensagem de erro</param>
        /// <param name="exitCode">Código de saída</param>
        /// <returns>Mensagem amigável para o usuário</returns>
        private string ParseNqcError(string errorMessage, int exitCode)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return "Erro desconhecido.";
            }

            string lowerError = errorMessage.ToLowerInvariant();

            if (exitCode == -13) return errorMessage; // Erros de compilação específicos

            if (lowerError.Contains("port") || lowerError.Contains("serial") || lowerError.Contains("com"))
                return "Erro ao acessar a porta serial.";

            if (lowerError.Contains("timeout") || lowerError.Contains("no response") || lowerError.Contains("not responding"))
                return "Robô não responde.";

            if (lowerError.Contains("firmware") || lowerError.Contains("version"))
                return "Firmware do robô incompatível.";

            return $"Erro na operação (código: {exitCode}).";
        }

        /// <summary>
        /// Verifica se o erro é relacionado a conexão (para permitir retry)
        /// </summary>
        /// <param name="result">Resultado da operação</param>
        /// <returns>True se for erro de conexão</returns>
        private static bool IsConnectionError(CompilationResult result)
        {
            if (result.Success) return false;

            string error = (result.ErrorDetails ?? string.Empty).ToLowerInvariant();
            string message = (result.Message ?? string.Empty).ToLowerInvariant();

            return error.Contains("timeout") ||
                   error.Contains("no response") ||
                   error.Contains("not responding") ||
                   error.Contains("connection") ||
                   message.Contains("não responde") ||
                   message.Contains("timeout") ||
                   message.Contains("robô não responde");
        }
    }

    // Helper para arquivo temporário com auto-delete
    internal sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(string content)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"roboblocos_{Guid.NewGuid()}.nqc");
            var encodingSemBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(Path, content, encodingSemBOM);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    /// <summary>
    /// Representa o resultado de uma compilação e envio para o robô
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Indica se a operação foi bem-sucedida
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensagem amigável para o usuário
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Detalhes técnicos do erro (se houver)
        /// </summary>
        public string ErrorDetails { get; set; } = string.Empty;

        /// <summary>
        /// Código de saída do processo NQC
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Saída completa do processo (stdout)
        /// </summary>
        public string Output { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa o progresso da compilação e envio
    /// </summary>
    public class CompilationProgress
    {
        /// <summary>
        /// Estágio atual da operação
        /// </summary>
        public CompilationStage Stage { get; set; }

        /// <summary>
        /// Mensagem de progresso
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Tentativa atual
        /// </summary>
        public int CurrentAttempt { get; set; }

        /// <summary>
        /// Total de tentativas
        /// </summary>
        public int TotalAttempts { get; set; }
    }

    /// <summary>
    /// Estágios da compilação
    /// </summary>
    public enum CompilationStage
    {
        Validating,      // Validando entrada
        Compiling,       // Compilando código
        Downloading,     // Enviando para o robô
        Success,         // Sucesso
        Failed           // Falha
    }
}
