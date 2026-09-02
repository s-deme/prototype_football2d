using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string root = args.Length == 0 ? "Assets" : args[0];
int errors = 0;
foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
{
    SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
    foreach (Diagnostic diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.Error.WriteLine(diagnostic);
        errors++;
    }
}

if (errors > 0) return 1;
Console.WriteLine("C# syntax check passed.");
return 0;

