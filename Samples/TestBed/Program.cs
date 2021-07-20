﻿////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                    //
//                                                                                                    //
//     NOTE: This is just my scratch pad for quickly testing stuff, not for human consumption         //
//                                                                                                    //
//                                                                                                    //
////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Reflection;
using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using static LanguageExt.Prelude;
using static LanguageExt.Pipes.Proxy;
using LanguageExt.ClassInstances;
using LanguageExt.TypeClasses;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LanguageExt.Effects;
using LanguageExt.Effects.Traits;
using LanguageExt.Pipes;
using LanguageExt.Sys;
using LanguageExt.Sys.Live;
using LanguageExt.Sys.Traits;
using LanguageExt.Sys.IO;
using TestBed;

public class Program
{
    static async Task Main(string[] args)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                    //
        //                                                                                                    //
        //     NOTE: This is just my scratch pad for quickly testing stuff, not for human consumption         //
        //                                                                                                    //
        //                                                                                                    //
        ///////////////////////////////////////////v////////////////////////////////////////////////////////////

        await PipesTest();

        // await ObsAffTests.Test();
        // await AsyncTests();
    }

    public static async Task PipesTest()
    {
        var effect = readLine | sayHello | writeLine;

        var result = await effect.RunEffect<Runtime, Unit>()
                                 .Run(Runtime.New());
    }
    
    
    static Producer<Runtime, string, Unit> readLine =>
        from w in liftIO(Console<Runtime>.writeLine("Enter your name"))
        from l in liftIO(Console<Runtime>.readLine)
        from _ in yield(l)
        from n in readLine
        select unit;

    static Pipe<Runtime, string, string, Unit> sayHello =>
        from l in awaiting<string>()         
        from _ in yield($"Hello {l}")
        from n in sayHello
        select unit;
    
    static Consumer<Runtime, string, Unit> writeLine =>
        from l in awaiting<string>()
        from a in liftIO(Console<Runtime>.writeLine(l))
        from n in writeLine 
        select unit;

    
    static Producer<Runtime, string, Unit> readLine2 =>
        from w in Producer.liftIO<Runtime, string, Unit>(Console<Runtime>.writeLine("Enter your name"))
        from l in Producer.liftIO<Runtime, string, string>(Console<Runtime>.readLine)
        from _ in Producer.yield<Runtime, string>(l)
        from n in readLine2
        select unit;

    static Pipe<Runtime, string, string, Unit> sayHello2 =>
        from l in Pipe.await<Runtime, string, string>()         
        from _ in Pipe.yield<Runtime, string, string>($"Hello {l}")
        from n in sayHello2
        select unit;
    
    static Consumer<Runtime, string, Unit> writeLine2 =>
        from l in Consumer.await<Runtime, string>()
        from a in Consumer.liftIO<Runtime, string>(Console<Runtime>.writeLine(l))
        from n in writeLine2
        select unit;
    
    
    static Pipe<Runtime, string, string, Unit> pipeMap =>
        Pipe.map((string x) => $"Hello {x}");
    
}
