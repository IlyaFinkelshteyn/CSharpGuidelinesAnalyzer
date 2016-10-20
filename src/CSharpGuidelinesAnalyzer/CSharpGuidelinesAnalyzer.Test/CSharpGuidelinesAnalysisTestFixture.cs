using CSharpGuidelinesAnalyzer.Test.RoslynTestFramework;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace CSharpGuidelinesAnalyzer.Test
{
    public abstract class CSharpGuidelinesAnalysisTestFixture : AnalysisTestFixture
    {
        protected void VerifyGuidelineDiagnostic([NotNull] ParsedSourceCode source,
            [NotNull] [ItemNotNull] params string[] messages)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(messages, nameof(messages));

            string text = source.GetText();

            AnalyzerTestContext analyzerContext = new AnalyzerTestContext(text, LanguageNames.CSharp, null)
                .WithReferences(source.References)
                .WithFileName(source.Filename)
                .InValidationMode(source.ValidationMode);

            AssertDiagnostics(analyzerContext, messages);
        }
    }
}