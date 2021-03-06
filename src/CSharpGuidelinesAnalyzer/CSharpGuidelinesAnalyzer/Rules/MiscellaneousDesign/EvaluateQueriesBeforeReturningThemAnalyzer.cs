using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace CSharpGuidelinesAnalyzer.Rules.MiscellaneousDesign
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EvaluateQueriesBeforeReturningThemAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1250";

        private const string Title = "Evaluate LINQ queries before returning them";

        private const string OperationMessageFormat =
            "{0} '{1}' returns the result of a call to '{2}', which uses deferred execution.";

        private const string QueryMessageFormat = "{0} '{1}' returns the result of a query that uses deferred execution.";
        private const string Description = "Evaluate the result of a LINQ expression before returning it.";
        private const string Category = "Miscellaneous Design";

        [NotNull]
        private static readonly DiagnosticDescriptor OperationRule = new DiagnosticDescriptor(DiagnosticId, Title,
            OperationMessageFormat, Category, DiagnosticSeverity.Warning, true, Description,
            HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [NotNull]
        private static readonly DiagnosticDescriptor QueryRule = new DiagnosticDescriptor(DiagnosticId, Title, QueryMessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(OperationRule, QueryRule);

        [ItemNotNull]
        private static readonly ImmutableArray<string> LinqOperatorsDeferred = ImmutableArray.Create("Aggregate", "All", "Any",
            "Cast", "Concat", "Contains", "DefaultIfEmpty", "Except", "GroupBy", "GroupJoin", "Intersect", "Join", "OfType",
            "OrderBy", "OrderByDescending", "Range", "Repeat", "Reverse", "Select", "SelectMany", "SequenceEqual", "Skip",
            "SkipWhile", "Take", "TakeWhile", "ThenBy", "ThenByDescending", "Union", "Where", "Zip");

        [ItemNotNull]
        private static readonly ImmutableArray<string> LinqOperatorsImmediate = ImmutableArray.Create("AsEnumerable", "Average",
            "Count", "Distinct", "ElementAt", "ElementAtOrDefault", "Empty", "First", "FirstOrDefault", "Last", "LastOrDefault",
            "LongCount", "Max", "Min", "Single", "SingleOrDefault", "Sum", "ToArray", "ToDictionary", "ToList", "ToLookup");

        [NotNull]
        private const string QueryOperationName = "";

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterConditionalOperationBlockAction(c => c.SkipInvalid(AnalyzeCodeBlock));
        }

        private void AnalyzeCodeBlock(OperationBlockAnalysisContext context)
        {
            var method = context.OwningSymbol as IMethodSymbol;
            if (method == null || method.ReturnsVoid || !ReturnsEnumerable(method))
            {
                return;
            }

            var variableEvaluationCache = new Dictionary<ILocalSymbol, EvaluationResult>();

            foreach (IReturnStatement returnStatement in
                context.OperationBlocks.SelectMany(b => b.DescendantsAndSelf().OfType<IReturnStatement>()))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                AnalyzeReturnStatement(returnStatement, context, variableEvaluationCache);
            }
        }

        private static bool ReturnsEnumerable([NotNull] IMethodSymbol method)
        {
            return method.ReturnType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
                method.ReturnType.SpecialType == SpecialType.System_Collections_IEnumerable;
        }

        private void AnalyzeReturnStatement([NotNull] IReturnStatement returnStatement, OperationBlockAnalysisContext context,
            [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache)
        {
            if (!ReturnsConstant(returnStatement) && !IsYieldBreak(returnStatement))
            {
                AnalyzeReturnValue(returnStatement, context, variableEvaluationCache);
            }
        }

        private bool IsYieldBreak([NotNull] IReturnStatement returnStatement)
        {
            return returnStatement.ReturnedValue == null;
        }

        private static bool ReturnsConstant([NotNull] IReturnStatement returnStatement)
        {
            return returnStatement.ReturnedValue is ILiteralExpression;
        }

        private void AnalyzeReturnValue([NotNull] IReturnStatement returnStatement, OperationBlockAnalysisContext context,
            [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache)
        {
            EvaluationResult result = AnalyzeExpression(returnStatement.ReturnedValue, context.OperationBlocks,
                variableEvaluationCache, context.CancellationToken);

            if (result.IsConclusive && result.IsDeferred)
            {
                ReportDiagnosticAt(returnStatement, result.DeferredOperationName, context);
            }
        }

        [NotNull]
        private static EvaluationResult AnalyzeExpression([NotNull] IOperation expression,
            [ItemNotNull] ImmutableArray<IOperation> body,
            [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache, CancellationToken cancellationToken)
        {
            Guard.NotNull(expression, nameof(expression));
            Guard.NotNull(variableEvaluationCache, nameof(variableEvaluationCache));

            cancellationToken.ThrowIfCancellationRequested();

            var local = expression as ILocalReferenceExpression;
            if (local != null)
            {
                return AnalyzeLocalReference(body, variableEvaluationCache, cancellationToken, local);
            }

            var conditional = expression as IConditionalChoiceExpression;
            if (conditional != null)
            {
                return AnalyzeConditionalChoice(body, variableEvaluationCache, cancellationToken, conditional);
            }

            var queryExpressionSyntax = expression.Syntax as QueryExpressionSyntax;
            return queryExpressionSyntax != null ? EvaluationResult.Query : AnalyzeMemberInvocation(expression);
        }

        [NotNull]
        private static EvaluationResult AnalyzeLocalReference([ItemNotNull] ImmutableArray<IOperation> body,
            [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache, CancellationToken cancellationToken,
            [NotNull] ILocalReferenceExpression local)
        {
            var assignmentWalker = new VariableAssignmentWalker(local.Local, body, variableEvaluationCache, cancellationToken);
            assignmentWalker.VisitMethod();

            return assignmentWalker.Result;
        }

        [NotNull]
        private static EvaluationResult AnalyzeConditionalChoice([ItemNotNull] ImmutableArray<IOperation> body,
            [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache, CancellationToken cancellationToken,
            [NotNull] IConditionalChoiceExpression conditional)
        {
            EvaluationResult trueResult = AnalyzeExpression(conditional.IfTrueValue, body, variableEvaluationCache,
                cancellationToken);
            EvaluationResult falseResult = AnalyzeExpression(conditional.IfFalseValue, body, variableEvaluationCache,
                cancellationToken);

            return EvaluationResult.Unify(trueResult, falseResult);
        }

        [NotNull]
        private static EvaluationResult AnalyzeMemberInvocation([NotNull] IOperation expression)
        {
            var invocationWalker = new MemberInvocationWalker();
            invocationWalker.Visit(expression);

            return invocationWalker.Result;
        }

        private void ReportDiagnosticAt([NotNull] IReturnStatement returnStatement, [NotNull] string operationName,
            OperationBlockAnalysisContext context)
        {
            Location location = returnStatement.GetLocationForKeyword();
            ISymbol containingMember = context.OwningSymbol.GetContainingMember();
            string memberName = containingMember.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

            Diagnostic diagnostic = operationName == QueryOperationName
                ? Diagnostic.Create(QueryRule, location, containingMember.Kind, memberName)
                : Diagnostic.Create(OperationRule, location, containingMember.Kind, memberName, operationName);

            context.ReportDiagnostic(diagnostic);
        }

        private sealed class EvaluationResult
        {
            private EvaluationState evaluationState;

            [CanBeNull]
            private string deferredOperationNameOrNull;

            [NotNull]
            public string DeferredOperationName
            {
                get
                {
                    if (evaluationState != EvaluationState.Deferred)
                    {
                        throw new InvalidOperationException("Operation name is not available in non-deferred states.");
                    }

                    // ReSharper disable once AssignNullToNotNullAttribute
                    return deferredOperationNameOrNull;
                }
            }

            public bool IsConclusive => evaluationState != EvaluationState.Initial;

            public bool IsDeferred => evaluationState == EvaluationState.Deferred;

            [NotNull]
            public static readonly EvaluationResult Query = new EvaluationResult(EvaluationState.Deferred, QueryOperationName);

            public EvaluationResult()
            {
            }

            private EvaluationResult(EvaluationState state, [CanBeNull] string deferredOperationNameOrNull)
            {
                evaluationState = state;
                this.deferredOperationNameOrNull = deferredOperationNameOrNull;
            }

            public void SetImmediate()
            {
                evaluationState = EvaluationState.Immediate;
            }

            public void SetUnknown()
            {
                evaluationState = EvaluationState.Unknown;
            }

            public void SetDeferred([NotNull] string operationName)
            {
                Guard.NotNullNorWhiteSpace(operationName, nameof(operationName));

                evaluationState = EvaluationState.Deferred;
                deferredOperationNameOrNull = operationName;
            }

            public void CopyIfConclusiveFrom([NotNull] EvaluationResult result)
            {
                Guard.NotNull(result, nameof(result));

                if (result.IsConclusive)
                {
                    CopyFrom(result);
                }
            }

            public void CopyFrom([NotNull] EvaluationResult result)
            {
                Guard.NotNull(result, nameof(result));

                evaluationState = result.evaluationState;
                deferredOperationNameOrNull = result.deferredOperationNameOrNull;
            }

            [NotNull]
            public static EvaluationResult Unify([NotNull] EvaluationResult first, [NotNull] EvaluationResult second)
            {
                Guard.NotNull(first, nameof(first));
                Guard.NotNull(second, nameof(second));

                if (first.IsConclusive && first.IsDeferred)
                {
                    return first;
                }
                if (second.IsConclusive && second.IsDeferred)
                {
                    return second;
                }

                return first.IsConclusive ? first : second;
            }

            public override string ToString()
            {
                return evaluationState.ToString();
            }

            private enum EvaluationState
            {
                Initial,
                Unknown,
                Immediate,
                Deferred
            }
        }

        private abstract class LinqOperationWalker : OperationWalker
        {
            [NotNull]
            public EvaluationResult Result { get; } = new EvaluationResult();
        }

        /// <summary>
        /// Analyzes method invocations in an expression, to determine whether the final expression is based on deferred or
        /// immediate execution.
        /// </summary>
        private sealed class MemberInvocationWalker : LinqOperationWalker
        {
            public override void VisitInvocationExpression([NotNull] IInvocationExpression operation)
            {
                base.VisitInvocationExpression(operation);

                if (operation.Instance == null)
                {
                    if (LinqOperatorsDeferred.Contains(operation.TargetMethod.Name))
                    {
                        Result.SetDeferred(operation.TargetMethod.Name);
                        return;
                    }
                    if (LinqOperatorsImmediate.Contains(operation.TargetMethod.Name))
                    {
                        Result.SetImmediate();
                        return;
                    }
                }

                Result.SetUnknown();
            }
        }

        /// <summary>
        /// Analyzes all assignments to a specific variable, storing the final state.
        /// </summary>
        private sealed class VariableAssignmentWalker : LinqOperationWalker
        {
            [NotNull]
            private readonly ILocalSymbol currentLocal;

            [ItemNotNull]
            private readonly ImmutableArray<IOperation> body;

            [NotNull]
            private readonly IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache;

            private readonly CancellationToken cancellationToken;

            public VariableAssignmentWalker([NotNull] ILocalSymbol local, [ItemNotNull] ImmutableArray<IOperation> body,
                [NotNull] IDictionary<ILocalSymbol, EvaluationResult> variableEvaluationCache, CancellationToken cancellationToken)
            {
                Guard.NotNull(local, nameof(local));
                Guard.NotNull(variableEvaluationCache, nameof(variableEvaluationCache));

                currentLocal = local;
                this.body = body;
                this.variableEvaluationCache = variableEvaluationCache;
                this.cancellationToken = cancellationToken;
            }

            public void VisitMethod()
            {
                if (variableEvaluationCache.ContainsKey(currentLocal))
                {
                    Result.CopyFrom(variableEvaluationCache[currentLocal]);
                }
                else
                {
                    foreach (IOperation operation in body)
                    {
                        Visit(operation);
                    }

                    variableEvaluationCache[currentLocal] = Result;
                }
            }

            public override void VisitVariableDeclarationStatement([NotNull] IVariableDeclarationStatement operation)
            {
                base.VisitVariableDeclarationStatement(operation);

                foreach (IVariableDeclaration variable in operation.Variables)
                {
                    if (currentLocal.Equals(variable.Variable))
                    {
                        AnalyzeAssignmentValue(variable.InitialValue);
                    }
                }
            }

            public override void VisitAssignmentExpression([NotNull] IAssignmentExpression operation)
            {
                base.VisitAssignmentExpression(operation);

                var targetLocal = operation.Target as ILocalReferenceExpression;
                if (targetLocal != null && currentLocal.Equals(targetLocal.Local))
                {
                    AnalyzeAssignmentValue(operation.Value);
                }
            }

            private void AnalyzeAssignmentValue([NotNull] IOperation assignedValue)
            {
                Guard.NotNull(assignedValue, nameof(assignedValue));

                EvaluationResult result = AnalyzeExpression(assignedValue, body, variableEvaluationCache, cancellationToken);
                Result.CopyIfConclusiveFrom(result);
            }
        }
    }
}
