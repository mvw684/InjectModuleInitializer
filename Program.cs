/* 
InjectModuleInitializer

Command line program to inject a module initializer into a .NET assembly.

Copyright (C) 2009-2016 Einar Egilsson
http://einaregilsson.com/module-initializers-in-csharp/

This program is licensed under the MIT license: http://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
//using EinarEgilsson.Utilities.InjectModuleInitializer.Test;
using System.Reflection;
using System.Security.AccessControl;

namespace EinarEgilsson.Utilities.InjectModuleInitializer
{
    internal class Program
    {
        static int Main(string[] args)
        {
//#if DEBUG
//            //I only have VS Express at home and this is the easiest way to debug
//            //the unit tests since there's no test runner and I can't attach to NUnit.
//            if (args.Length == 1 && args[0] == "/runtests")
//            {
//                return TestRunner.RunTests();
//            }
//#endif
            var injector = new Injector();
            if (args.Length == 0 || args.Length > 4 || Regex.IsMatch(args[0], @"^((/|--?)(\?|h|help))$"))
            {
                PrintHelp();
                return 1;
            }

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("InjectModuleInitializer v{0}.{1}", version.Major, version.Minor);
            Console.WriteLine("");
            List<string> additionalAssemblySearchPath = new List<string>();
            string assemblyFile, moduleInitializer=null, keyfile=null;
            assemblyFile = args[args.Length - 1];

            for (int i = 0; i < args.Length - 1; i++)
            {
                var initMatch = Regex.Match(args[i], "^/m(oduleinitializer)?:(.+)", RegexOptions.IgnoreCase);
                if (initMatch.Success)
                {
                    moduleInitializer = initMatch.Groups[2].Value;
                }
                var keyMatch = Regex.Match(args[i], "^/k(eyfile)?:(.+)", RegexOptions.IgnoreCase);
                if (keyMatch.Success)
                {
                    keyfile = keyMatch.Groups[2].Value;
                }

                var assemblySearchPathMatch = Regex.Match(args[i], "^/a(ssemnbySearchPath)?:(.+)", RegexOptions.IgnoreCase);
                if (assemblySearchPathMatch.Success)
                {
                    var tempPath = assemblySearchPathMatch.Groups[2].Value;
                    var temPaths = tempPath.Split(new char[] {';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var path in temPaths)
                    {
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            additionalAssemblySearchPath.Add(path);
                        }
                    }

                }
                if (!initMatch.Success && !keyMatch.Success && !assemblySearchPathMatch.Success)
                {
                    Console.Error.WriteLine("error: Invalid argument '{0}', type InjectModuleInitializer /? for help", args[0]);
                    return 1;
                }
            }

            try
            {
                injector.Inject(assemblyFile, additionalAssemblySearchPath, moduleInitializer, keyfile);
                Console.WriteLine("Module Initializer successfully injected in assembly " + assemblyFile);
                return 0;
            }
            catch (InjectionException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
            finally
            {
                injector.Dispose();
            }
        }
        
        static void PrintHelp()
        {
            Console.Error.WriteLine(@"
InjectModuleInitializer.exe [/m:<method>] [/k:<keyfile>] filename

/m:<method>                   Specify the method to be run as the module  
/moduleinitializer:<method>   initializer. Written as full name of containing
                              type, followed by :: and then the method name,
                              e.g. Namespace.ClassName::Method. Method is
                              required to be a public or internal static
                              method that takes no parameters and returns void.
                              If this parameter is omitted the program will 
                              look for a type name ModuleInitializer (in any
                              namespace) and look for a method named Run in
                              that type.
/a:<assemblySearchPath>       Specify additional paths to search referenced assemblies
/assemblySearchPath:<assemblySearchPath>
                              Each entry can be a semicolon separated 'path'
                              This argument can occur multiple times.
/k:<keyfile>                  A strong name key file that will be used to sign
/keyfile:<keyfile>            the assembly after the module initializer is
                              injected into it.

filename                      Name of the assembly file (exe or dll) to inject
                              a module initializer into.

/?                            Prints this help screen.

Additional information about this program can be found at the url:

  http://einaregilsson.com/module-initializers-in-csharp/
");
        }

    }
}
