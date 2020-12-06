using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = CodeAnalysis.ParameterWrapper.Test.CSharpCodeFixVerifier<
    CodeAnalysis.ParameterWrapper.ParameterWrapperAnalyzer,
    CodeAnalysis.ParameterWrapper.ParameterWrapperCodeFixProvider>;

namespace CodeAnalysis.ParameterWrapper.Test
{
    [TestClass]
    public class WellOrganizedConstructorTests
    {
        [TestMethod]
        public async Task WellOrganizedCtor_Args0()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A()
        {
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task WellOrganizedCtor_Args1()
        {
            var test = @"
namespace Test
{
    class A
    {   
        public A(int a)
        {
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task WellOrganizedCtor_Args2()
        {
            var test = @"
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
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
