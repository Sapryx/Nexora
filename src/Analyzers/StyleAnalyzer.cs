using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

public abstract class StyleAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];
    private readonly DiagnosticDescriptor rule;

    protected StyleAnalyzer(string id, string title, string messageFormat)
    {
        rule = new DiagnosticDescriptor(id, title, messageFormat, "Style", DiagnosticSeverity.Warning, true);
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        Register(context);
    }

    protected abstract void Register(AnalysisContext context);

    protected void Report(SyntaxNodeAnalysisContext context, Location location, params object[] arguments)
    {
        context.ReportDiagnostic(Diagnostic.Create(rule, location, arguments));
    }

    protected void Report(SyntaxTreeAnalysisContext context, Location location, params object[] arguments)
    {
        context.ReportDiagnostic(Diagnostic.Create(rule, location, arguments));
    }
}
