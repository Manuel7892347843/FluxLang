using System.Diagnostics;

namespace FluxLang;

class Program
{
    static void Main(string[] args)
    {
        string fileNameFull = args.Length > 0 ? args[0] : throw new Exception("No file specified");
        if (!File.Exists(fileNameFull)) throw new Exception("File not found");
        string code = File.ReadAllText(fileNameFull);
        
        string fileName = Path.GetFileNameWithoutExtension(fileNameFull);
        string directory = Path.GetDirectoryName(fileNameFull) ?? ".";
        
        string outDir = Path.Combine(directory, "out");
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        
        string baseTempPath = Path.Combine(outDir, fileName);
        
        string exeName = GetArgValue(args, "-exe", "program");
        bool devMode = GetArgValue(args, "-dev", "false") == "true";
        
        //1. Lexer
        Lexer lexer = new Lexer(code);
        List<Token> tokens = lexer.Tokenize();
        
        //2. Parser
        Parser.ClearImportCache();
        Parser parser = new Parser(tokens, fileNameFull);
        ProgramNode ast = parser.Parse();
        
        //3. Code Generator
        CodeGenerator generator = new CodeGenerator(ast);
        string asseblyCode = generator.Generate();
        
        //4. Saves the generated code file
        string asmPath = $"{baseTempPath}.asm";
        string objPath = $"{baseTempPath}.o";
        
        File.WriteAllText(asmPath, asseblyCode);
        if(devMode)
            Console.WriteLine($"[1/4] File {asmPath} generated.");
        
        //5. Terminal automation: NASM execution
        ExecuteCommands("nasm", $"-f macho64 {asmPath} -o {objPath}");
        if(devMode)
            Console.WriteLine($"[2/4] NASM: Object file {objPath} generated.");
        
        string exeDir = Path.Combine(directory, "exe");
        if (!Directory.Exists(exeDir))
        {
            Directory.CreateDirectory(exeDir);
        }
        
        string exePath = Path.Combine(exeDir, fileName);

        ExecuteCommands("clang", $"-arch x86_64 {objPath} -o {exePath}");
        if(devMode)
            Console.WriteLine($"[3/4] Clang: Linking completed. Executable {exePath} ready.");
        
        //7. Execute your program and shows the output in real time
        if (devMode)
        {
            Console.WriteLine("[4/4] Execution of your program started...\n");
            Console.WriteLine("--------------------------------------------");   
        }
        
        ExecuteNativeExe(exePath);
        
        if(devMode)
            Console.WriteLine("--------------------------------------------");
    }

    private static void ExecuteCommands(string command, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            string error = process?.StandardError.ReadToEnd() ?? "Unknown error";
            throw new Exception($"Error during the execution of {command}: {error}");
        }
    }

    private static void ExecuteNativeExe(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = path,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
            
        if (process != null && process.ExitCode != 0)
        {
             Console.WriteLine($"\n[PROCESS FINISHED] Exit Code: {process.ExitCode}");
        }
    }

    private static string GetArgValue(string[] args, string flag, string defaultValue = "")
    {
        int idx = Array.IndexOf(args, flag);
        if(idx != -1 && idx + 1 < args.Length)
            return args[idx + 1];
        return defaultValue;
    }
}