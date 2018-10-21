﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslynator.CommandLine
{
    internal static class AssemblyResolver
    {
        static AssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        internal static void Register()
        {
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            if (assemblyName.Name.EndsWith(".resources"))
                return null;
#if DEBUG
            Console.WriteLine($"Resolve assembly '{args.Name}'");
#endif
            switch (assemblyName.Name)
            {
                case "Microsoft.CodeAnalysis":
                case "Microsoft.CodeAnalysis.CSharp":
                case "Microsoft.CodeAnalysis.CSharp.Workspaces":
                case "Microsoft.CodeAnalysis.VisualBasic":
                case "Microsoft.CodeAnalysis.VisualBasic.Workspaces":
                case "Microsoft.CodeAnalysis.Workspaces":
                case "System.Collections.Immutable":
                case "System.Composition.AttributedModel":
                case "System.Composition.Convention":
                case "System.Composition.Hosting":
                case "System.Composition.Runtime":
                case "System.Composition.TypedParts":
                    {
                        Assembly assembly = FindLoadedAssembly();

                        if (assembly != null)
                            return assembly;

                        break;
                    }
            }

            //TODO: throw?
            throw new InvalidOperationException($"Unable to resolve assembly '{assemblyName.FullName}'.");

            Assembly FindLoadedAssembly()
            {
                Assembly result = null;

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AssemblyName an = assembly.GetName();

                    if (assemblyName.Name == an.Name
                        && assemblyName.Version <= an.Version)
                    {
                        if (result == null)
                        {
                            result = assembly;
                        }
                        else if (result.GetName().Version < an.Version)
                        {
                            result = assembly;
                        }
                    }
                }

                return result;
            }
        }
    }
}