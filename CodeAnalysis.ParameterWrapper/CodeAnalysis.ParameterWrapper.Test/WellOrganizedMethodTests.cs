using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = CodeAnalysis.ParameterWrapper.Test.CSharpCodeFixVerifier<
    CodeAnalysis.ParameterWrapper.ParameterWrapperAnalyzer,
    CodeAnalysis.ParameterWrapper.ParameterWrapperCodeFixProvider>;

namespace CodeAnalysis.ParameterWrapper.Test
{
    [TestClass]
    public class WellOrganizedMethodTests
    {
        [TestMethod]
        public async Task WellOrganizedMethod_Args0()
        {
            var test = @"
namespace Test
{
    class A
    {   
        void Foo()
        {
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task WellOrganizedMethod_Args1()
        {
            var test = @"
namespace Test
{
    class A
    {   
        void Foo(int a)
        {
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task WellOrganizedMethod_Args2()
        {
            var test = @"
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
