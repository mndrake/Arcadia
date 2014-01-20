
#if INTERACTIVE
#r "bin/debug/Arcadia.dll"
#r "bin/debug/Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll"
#r "bin/debug/Foq.dll"
#else
namespace Arcadia.UnitTests
#endif

open Foq
open Arcadia
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type Test_CalculationHandler() =
    [<TestMethod>]        
    member this.Test1() =
        Assert.IsTrue(true)