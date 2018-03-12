﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings.If
{
    internal sealed class AssignmentAndIfElseToAssignmentWithConditionalExpressionAnalysis : ToAssignmentWithConditionalExpressionAnalysis<ExpressionStatementSyntax>
    {
        internal AssignmentAndIfElseToAssignmentWithConditionalExpressionAnalysis(
            ExpressionStatementSyntax statement,
            ExpressionSyntax right,
            IfStatementSyntax ifStatement,
            ExpressionSyntax whenTrue,
            ExpressionSyntax whenFalse) : base(statement, ifStatement, whenTrue, whenFalse)
        {
            Left = right;
        }

        public ExpressionSyntax Left { get; }

        public override IfRefactoringKind Kind
        {
            get { return IfRefactoringKind.AssignmentAndIfElseToAssignmentWithConditionalExpression; }
        }
    }
}
