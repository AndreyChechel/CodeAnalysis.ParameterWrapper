using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = CodeAnalysis.ParameterWrapper.Test.CSharpCodeFixVerifier<
    CodeAnalysis.ParameterWrapper.ParameterWrapperAnalyzer,
    CodeAnalysis.ParameterWrapper.ParameterWrapperCodeFixProvider>;

namespace CodeAnalysis.ParameterWrapper.Test
{
    [TestClass]
    public class MethodCodeFixTests
    {
        [TestMethod]
        public async Task MethodParameterListOnNewLine_Args0()
        {
            var test = @"
namespace Test
{
    class A
    {   
        void Foo
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
        void Foo()
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task MethodParameterListOnNewLine_Args1()
        {
            var test = @"
namespace Test
{
    class A
    {   
        void Foo
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
        void Foo(int a)
        {
        }
    }
}";

            await Verify(test, fixtest);
        }

        [TestMethod]
        public async Task MethodParameterListOnNewLine_Args2()
        {
            var test = @"
namespace Test
{
    class A
    {   
        void Foo
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
        void Foo
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

        private static async Task Verify(string test, string fixtest, int key = 0, string name = "Foo")
        {
            var expected = VerifyCS.Diagnostic("ParameterWrapperAnalyzer").WithLocation(key).WithArguments(name);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
