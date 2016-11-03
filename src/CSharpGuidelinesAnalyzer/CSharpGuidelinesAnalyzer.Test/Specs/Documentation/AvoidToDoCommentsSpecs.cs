using CSharpGuidelinesAnalyzer.Documentation;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Documentation
{
    public class AvoidToDoCommentsSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => AvoidToDoCommentsAnalyzer.DiagnosticId;

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_colon_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // [|TODO:test|]
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_space_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // [|TODO test|]
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_underscore_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // TODO_test
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_number_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // TODO1 test
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_quotes_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // ""TODO test""
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_source_contains_single_line_comment_with_todo_in_the_middle_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    // Hello TODO test
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_source_contains_documentation_comment_with_space_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    ///    [|TODO test|]
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void
            When_source_contains_preprocessor_directive_with_single_line_todo_comment_with_space_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    #if DEBUG // [|TODO test|]
                    #endif
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void
            When_source_contains_preprocessor_directive_with_documentation_comment_with_space_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    #if DEBUG ///    [|TODO test|]
                    #endif
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_region_directive_with_single_line_todo_comment_with_space_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    #region // TODO test
                    #endregion
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void
            When_source_contains_end_region_directive_with_single_line_todo_comment_with_space_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    #region
                    #endregion // [|TODO test|]
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_single_line_todo_comment_with_trailing_spaces_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"// [|TODO test                        |]")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_multi_line_todo_comment_on_single_line_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    /* [|TODO: hello    |]*/
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_multi_line_documentation_comment_on_single_line_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    /** [|TODO: hello    |]*/
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_multi_line_todo_comment_on_multiple_lines_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    /* [|TODO: hello    |]
                            [|TODO: hello    |]
                    [|TODO: hello    |]
                        * [|TODO: hello    |]
                        [|TODO: hello    |]*/
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_multi_line_documentation_comment_on_multiple_lines_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    /** [|TODO: hello    |]
                            [|TODO: hello    |]
                    [|TODO: hello    |]
                        * [|TODO: hello    |]
                        [|TODO: hello    |]*/
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.",
                "Work tracking comment should be removed.");
        }

        [Fact]
        public void When_source_contains_single_line_documentation_comment_on_multiple_lines_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    /// <summary>
                    /// [|TODO : test       |]
                    /// </summary>
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Work tracking comment should be removed.");
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new AvoidToDoCommentsAnalyzer();
        }
    }
}