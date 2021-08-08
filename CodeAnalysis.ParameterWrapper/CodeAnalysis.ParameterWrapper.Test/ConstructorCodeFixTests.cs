using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = CodeAnalysis.ParameterWrapper.Test.CSharpCodeFixVerifier<
    CodeAnalysis.ParameterWrapper.ParameterWrapperAnalyzer,
    CodeAnalysis.ParameterWrapper.ParameterWrapperCodeFixProvider>;

namespace CodeAnalysis.ParameterWrapper.Test
{
    [TestClass]
    public class ConstructorCodeFixTests
    {
        [TestMethod]
        public async Task CtorParameterListOnNewLine_Args0()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A
        {|#0:()|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A()
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task CtorParameterListOnMultipleLines_Args0()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A
        {|#0:(
        )|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A()
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task CtorParameterListOnNewLine_Args1()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A
        {|#0:(int a)|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A(int a)
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task CtorParameterListOnMultipleLines_Args1()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A
        {|#0:(
            int a
        )|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A(int a)
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task CtorParameterListOnNewLine_Args2()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A
        {|#0:(int a, int b)|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A
        (
            int a,
            int b
        )
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task CtorParameterListOnMultipleLines_Args2()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A{|#0:(
        int a, 
        int b)|}
        {
        }
    }
}";

            var fixtest = @"
namespace Test
{
    class A
    {   
        public A
        (
            int a,
            int b
        )
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        private static async Task Verify(string test, string fixtest, int key = 0, string name = "A.ctor")
        {
            var expected = VerifyCS.Diagnostic("ParameterWrapperAnalyzer").WithLocation(key).WithArguments(name);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
