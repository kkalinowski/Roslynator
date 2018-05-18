﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CodeFixes;
using Roslynator.CSharp.Analysis;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;
using static Roslynator.CSharp.SyntaxInfo;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OptimizeLinqMethodCallCodeFixProvider))]
    [Shared]
    public class OptimizeLinqMethodCallCodeFixProvider : BaseCodeFixProvider
    {
        private const string Title = "Optimize LINQ method call";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.OptimizeLinqMethodCall); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out SyntaxNode node, predicate: f => f.IsKind(
                SyntaxKind.InvocationExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.IsPatternExpression)))
            {
                return;
            }

            Diagnostic diagnostic = context.Diagnostics[0];

            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    {
                        var invocation = (InvocationExpressionSyntax)node;

                        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

                        string name = memberAccess.Name.Identifier.ValueText;

                        if (name == "Cast")
                        {
                            CodeAction codeAction = CodeAction.Create(
                                Title,
                                cancellationToken => CallOfTypeInsteadOfWhereAndCastAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                        else if (name == "Any"
                            && invocation.ArgumentList.Arguments.Count == 1)
                        {
                            CodeAction codeAction = CodeAction.Create(
                                Title,
                                cancellationToken => CombineEnumerableWhereAndAnyAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                        else if (name == "OfType")
                        {
                            CodeAction codeAction = CodeAction.Create(
                                Title,
                                cancellationToken => OptimizeOfTypeAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                        else if (name == "Select")
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Call 'Cast' instead of 'Select'",
                                cancellationToken => CallCastInsteadOfSelectAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                        else if (name == "FirstOrDefault"
                            && invocation.ArgumentList.Arguments.Any())
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Call 'Find' instead of 'FirstOrDefault'",
                                cancellationToken => CallFindInsteadOfFirstOrDefaultAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                        else if (name == "First"
                            && diagnostic.Properties.TryGetValue("MethodName", out string methodName)
                            && methodName == "Peek")
                        {
                            CodeAction codeAction = CodeAction.Create(
                                Title,
                                cancellationToken => context.Document.ReplaceNodeAsync(invocation, RefactoringUtility.ChangeInvokedMethodName(invocation, "Peek"), cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                        else
                        {
                            CodeAction codeAction = CodeAction.Create(
                                Title,
                                cancellationToken => SimplifyLinqMethodChainAsync(context.Document, invocation, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                        }

                        break;
                    }
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.IsPatternExpression:
                    {
                        CodeAction codeAction = CodeAction.Create(
                            Title,
                            cancellationToken => SimplifyNullChckWithFirstOrDefaultAsync(context.Document, node, cancellationToken),
                            base.GetEquivalenceKey(diagnostic));

                        context.RegisterCodeFix(codeAction, diagnostic);
                        break;
                    }
            }
        }

        private static Task<Document> CallOfTypeInsteadOfWhereAndCastAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

            var memberAccess2 = (MemberAccessExpressionSyntax)invocation2.Expression;

            var genericName = (GenericNameSyntax)memberAccess.Name;

            InvocationExpressionSyntax newInvocation = invocation2.Update(
                memberAccess2.WithName(genericName.WithIdentifier(Identifier("OfType"))),
                invocation.ArgumentList.WithArguments(SeparatedList<ArgumentSyntax>()));

            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }

        private static Task<Document> CombineEnumerableWhereAndAnyAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SimpleMemberInvocationExpressionInfo(invocationExpression);
            SimpleMemberInvocationExpressionInfo invocationInfo2 = SimpleMemberInvocationExpressionInfo(invocationInfo.Expression);

            SingleParameterLambdaExpressionInfo lambda = SingleParameterLambdaExpressionInfo((LambdaExpressionSyntax)invocationInfo.Arguments.First().Expression);
            SingleParameterLambdaExpressionInfo lambda2 = SingleParameterLambdaExpressionInfo((LambdaExpressionSyntax)invocationInfo2.Arguments.First().Expression);

            BinaryExpressionSyntax logicalAnd = LogicalAndExpression(
                ((ExpressionSyntax)lambda2.Body).Parenthesize(),
                ((ExpressionSyntax)lambda.Body).Parenthesize());

            InvocationExpressionSyntax newNode = invocationInfo2.InvocationExpression
                .ReplaceNode(invocationInfo2.Name, invocationInfo.Name.WithTriviaFrom(invocationInfo2.Name))
                .WithArgumentList(invocationInfo2.ArgumentList.ReplaceNode((ExpressionSyntax)lambda2.Body, logicalAnd));

            return document.ReplaceNodeAsync(invocationExpression, newNode, cancellationToken);
        }

        private static Task<Document> SimplifyLinqMethodChainAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

            var memberAccess2 = (MemberAccessExpressionSyntax)invocation2.Expression;

            InvocationExpressionSyntax newNode = invocation2.WithExpression(
                memberAccess2.WithName(memberAccess.Name.WithTriviaFrom(memberAccess2.Name)));

            IEnumerable<SyntaxTrivia> trivia = invocation.DescendantTrivia(TextSpan.FromBounds(invocation2.Span.End, invocation.Span.End));

            if (trivia.Any(f => !f.IsWhitespaceOrEndOfLineTrivia()))
            {
                newNode = newNode.WithTrailingTrivia(trivia.Concat(invocation.GetTrailingTrivia()));
            }
            else
            {
                newNode = newNode.WithTrailingTrivia(invocation.GetTrailingTrivia());
            }

            return document.ReplaceNodeAsync(invocation, newNode, cancellationToken);
        }

        private static Task<Document> SimplifyNullChckWithFirstOrDefaultAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            NullCheckExpressionInfo nullCheck = NullCheckExpressionInfo(node, NullCheckStyles.ComparisonToNull | NullCheckStyles.IsNull);

            var invocation = (InvocationExpressionSyntax)nullCheck.Expression;

            ExpressionSyntax newNode = RefactoringUtility.ChangeInvokedMethodName(invocation, "Any");

            if (node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.IsPatternExpression))
                newNode = LogicalNotExpression(newNode.TrimTrivia().Parenthesize());

            newNode = newNode.WithTriviaFrom(node);

            return document.ReplaceNodeAsync(node, newNode, cancellationToken);
        }

        private static async Task<Document> OptimizeOfTypeAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo invocationInfo = SimpleMemberInvocationExpressionInfo(invocationExpression);

            TypeSyntax typeArgument = ((GenericNameSyntax)invocationInfo.Name).TypeArgumentList.Arguments.Single();

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            ExpressionSyntax newNode;
            if (semanticModel.GetTypeSymbol(typeArgument, cancellationToken).IsValueType)
            {
                newNode = invocationInfo.Expression.WithTrailingTrivia(invocationExpression.GetTrailingTrivia());
            }
            else
            {
                newNode = invocationExpression
                    .WithExpression(invocationInfo.MemberAccessExpression.WithName(IdentifierName("Where").WithTriviaFrom(invocationInfo.Name)))
                    .AddArgumentListArguments(
                        Argument(
                            SimpleLambdaExpression(
                                Parameter(Identifier(DefaultNames.LambdaParameter)),
                                NotEqualsExpression(
                                    IdentifierName(DefaultNames.LambdaParameter),
                                    NullLiteralExpression()
                                )
                            ).WithFormatterAnnotation()
                        )
                    );
            }

            return await document.ReplaceNodeAsync(invocationExpression, newNode, cancellationToken).ConfigureAwait(false);
        }

        private static Task<Document> CallCastInsteadOfSelectAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            ArgumentSyntax lastArgument = invocationExpression.ArgumentList.Arguments.Last();

            var lambdaExpression = (LambdaExpressionSyntax)lastArgument.Expression;

            GenericNameSyntax newName = GenericName(
                Identifier("Cast"),
                CallCastInsteadOfSelectAnalysis.GetCastExpression(lambdaExpression.Body).Type);

            InvocationExpressionSyntax newInvocationExpression = invocationExpression
                .RemoveNode(lastArgument)
                .WithExpression(memberAccessExpression.WithName(newName));

            return document.ReplaceNodeAsync(invocationExpression, newInvocationExpression, cancellationToken);
        }

        private static async Task<Document> CallFindInsteadOfFirstOrDefaultAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            SimpleMemberInvocationExpressionInfo info = SimpleMemberInvocationExpressionInfo(invocation);

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(info.Expression, cancellationToken);

            if ((typeSymbol as IArrayTypeSymbol)?.Rank == 1)
            {
                NameSyntax arrayName = ParseName("System.Array")
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithSimplifierAnnotation();

                MemberAccessExpressionSyntax newMemberAccess = SimpleMemberAccessExpression(
                    arrayName,
                    info.OperatorToken,
                    IdentifierName("Find").WithTriviaFrom(info.Name));

                ArgumentListSyntax argumentList = invocation.ArgumentList;

                InvocationExpressionSyntax newInvocation = InvocationExpression(
                    newMemberAccess,
                    ArgumentList(
                        Argument(info.Expression.WithoutTrivia()),
                        argumentList.Arguments.First()
                    ).WithTriviaFrom(argumentList));

                return await document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                IdentifierNameSyntax newName = IdentifierName("Find").WithTriviaFrom(info.Name);

                return await document.ReplaceNodeAsync(info.Name, newName, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}