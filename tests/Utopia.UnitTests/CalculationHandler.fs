
#if INTERACTIVE
#r "bin/debug/Utopia.dll"
#r "bin/debug/Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll"
#r "bin/debug/Foq.dll"
#else
namespace Utopia.UnitTests
#endif

open Foq
open Utopia
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type Test_CalculationHandler() =
    [<TestMethod>]        
    member this.Test1() =
        Assert.IsTrue(true)