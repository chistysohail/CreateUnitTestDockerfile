using System;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        // Get the folder path from the user
        Console.WriteLine("Enter the path to the main folder:");
        string mainFolderPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(mainFolderPath) || !Directory.Exists(mainFolderPath))
        {
            Console.WriteLine($"The directory '{mainFolderPath}' does not exist.");
            return;
        }

        // Get all .csproj files under the main folder and its subfolders
        var csprojFiles = Directory.GetFiles(mainFolderPath, "*.csproj", SearchOption.AllDirectories);

        // Filter the test projects (e.g., file names containing 'Test')
        var testCsprojFiles = csprojFiles.Where(file => Path.GetFileName(file).Contains("Test", StringComparison.OrdinalIgnoreCase)).ToList();

        if (!testCsprojFiles.Any())
        {
            Console.WriteLine("No test projects found.");
            return;
        }

        // Create Dockerfile
        string dockerFilePath = Path.Combine(mainFolderPath, "Dockerfile");
        using (StreamWriter writer = new StreamWriter(dockerFilePath))
        {
            writer.WriteLine("FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build");
            writer.WriteLine("WORKDIR /app");
            writer.WriteLine();

            // Copy and restore dependencies for each test project
            foreach (var testCsproj in testCsprojFiles)
            {
                string relativePath = Path.GetRelativePath(mainFolderPath, testCsproj);
                string projectFolder = Path.GetDirectoryName(relativePath);
                writer.WriteLine($"COPY [\"{relativePath}\", \"{projectFolder}/\"]");
                writer.WriteLine($"RUN dotnet restore \"{relativePath}\"");
            }

            writer.WriteLine();

            // Copy source files and build each test project
            foreach (var testCsproj in testCsprojFiles)
            {
                string relativePath = Path.GetRelativePath(mainFolderPath, testCsproj);
                string projectFolder = Path.GetDirectoryName(relativePath);
                writer.WriteLine($"COPY [\"{projectFolder}\", \"{projectFolder}\"]");
                writer.WriteLine($"RUN dotnet build \"{relativePath}\" -c Release -o /app/build");
            }

            writer.WriteLine();

            // Run tests for each test project
            foreach (var testCsproj in testCsprojFiles)
            {
                writer.WriteLine($"RUN dotnet test \"{testCsproj}\"");
            }

            writer.WriteLine();

            // Final stage/image
            writer.WriteLine("FROM mcr.microsoft.com/dotnet/runtime:6.0");
            writer.WriteLine("WORKDIR /app");
            writer.WriteLine("COPY --from=build /app/build .");
            writer.WriteLine();

            // Set the entry point for the application
            writer.WriteLine("ENTRYPOINT [\"dotnet\", \"YourConsoleApp.dll\"]");
        }

        Console.WriteLine($"Dockerfile has been generated successfully at: {dockerFilePath}");
    }
}
