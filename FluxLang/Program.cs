using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace FluxLang;

class Program
{
    static void Main(string[] args)
    {
        string os = GetArgValue(args, "-os", "");
        
        // 1. Estrae correttamente il percorso del file sorgente
        string fileNameFull = GetArgValue(args, "-file", "");

        if (string.IsNullOrEmpty(fileNameFull)) 
            throw new Exception("No file specified");
        
        if (!File.Exists(fileNameFull)) 
            throw new Exception($"File not found: {Path.GetFullPath(fileNameFull)}");

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
        
        //3. Code Generator (Passa la variabile 'os' al metodo Generate)
        CodeGenerator generator = new CodeGenerator(ast);
        string asseblyCode = generator.Generate(os);
        
        //4. Saves the generated code file
        string asmPath = $"{baseTempPath}.asm";
        string objPath = $"{baseTempPath}.o";
        
        File.WriteAllText(asmPath, asseblyCode);
        if(devMode)
            Console.WriteLine($"[1/4] File {asmPath} generated.");
        
        //5. Terminal automation: NASM execution
        // Windows : win64
        // MacOS   : macho64
        if (os == "win64")
            ExecuteCommands("nasm", $"-f win64 {asmPath} -o {objPath}");
        else if(os == "win32")
            ExecuteCommands("nasm", $"-f win32 {asmPath} -o {objPath}");
        else if(os == "macho64")
            ExecuteCommands("nasm", $"-f macho64 {asmPath} -o {objPath}");
        else
            throw new Exception("OS not supported");
        
        if(devMode)
            Console.WriteLine($"[2/4] NASM: Object file {objPath} generated.");
        
        string exeDir = Path.Combine(directory, "exe");
        if (!Directory.Exists(exeDir))
        {
            Directory.CreateDirectory(exeDir);
        }
        
        // Aggiunge automaticamente l'estensione .exe se siamo su piattaforma Windows
        string extension = (os == "win64" || os == "win32") ? ".exe" : "";
        string exePath = Path.Combine(exeDir, fileName + extension);

        // Windows : no -arch x86_64                             -Wl,-subsystem:console
        // MacOS   : -arch x86_64
        if(os == "win64" || os == "win32")
            ExecuteCommands("clang", $"{objPath} -o {exePath} -Wl,-subsystem:console");
        else if(os == "macho64")
            ExecuteCommands("clang", $"-arch x86_64 {objPath} -o {exePath}");
        else
            throw new Exception("OS not supported");
        
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
