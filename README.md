# ParameterWrapper
[![NuGet Status](https://img.shields.io/nuget/v/CodeAnalysis.ParameterWrapper)](https://www.nuget.org/packages/CodeAnalysis.ParameterWrapper/)

This analyzer helps to organize method/constructor parameters nicely in your C# program. 
You'll find it extremely helpful in projects of a certain size with DI containers configured, long type names and/or methods with many parameters declared.

## Example

Consider the following method declarations:

```cs
class Test
{   
    public void Foo(int a)
    {
    }
    
    public void Foo(int a, int b)
    {
    }
    
    public void Foo(int a, int b, int c)
    {
    }
}
```

Below is formatting the analyzer will suggest:

```cs
class Test
{   
    public void Foo(int a)
    {
    }
    
    public void Foo
    (
        int a,
        int b
    )
    {
    }
    
    public void Foo
    (
        int a,
        int b,
        int c
    )
    {
    }
}
```

## Demo

![ParameterWrapper](https://user-images.githubusercontent.com/16582701/101297384-d1423300-3839-11eb-8ad3-48a4354dfad2.gif)
