﻿// Copyright © .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Win32MetaGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private const string NativeMethodsAdditionalFileName = "NativeMethods.txt";
        private static readonly DiagnosticDescriptor NoMatchingMethod = new DiagnosticDescriptor(
            "PInvoke001",
            "No matching method",
            "Method \"{0}\" not found.",
            "Functionality",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor NoMethodsForModule = new DiagnosticDescriptor(
            "PInvoke001",
            "No module found",
            "No methods found under module \"{0}\".",
            "Functionality",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var generator = new Generator();
            AdditionalText? nativeMethodsFile = context.AdditionalFiles
                .FirstOrDefault(af => string.Equals(Path.GetFileName(af.Path), NativeMethodsAdditionalFileName, StringComparison.OrdinalIgnoreCase));
            if (nativeMethodsFile is null)
            {
                return;
            }

            SourceText? nativeMethodsTxt = nativeMethodsFile.GetText(context.CancellationToken);
            if (nativeMethodsTxt is null)
            {
                return;
            }

            foreach (TextLine line in nativeMethodsTxt.Lines)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                string name = line.ToString();
                var location = Location.Create(nativeMethodsFile.Path, line.Span, nativeMethodsTxt.Lines.GetLinePositionSpan(line.Span));
                if (name.EndsWith(".*"))
                {
                    var moduleName = name.Substring(0, name.Length - 2);
                    if (!generator.TryGenerateAllExternMethods(moduleName, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NoMethodsForModule, location, moduleName));
                    }
                }
                else
                {
                    if (!generator.TryGenerateExternMethod(name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NoMatchingMethod, location, name));
                    }
                }
            }

            context.AddSource("NativeMethods.cs", generator.CompilationUnit.ToFullString());
        }
    }
}