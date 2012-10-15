Telerik has built extensions for JustCode to demonstrate usage of the public JustCode API and to collect individual contributions for JustCode in one package. These extensions are open sourced and licensed under MS-PL. You are welcome to use this project as the basis for creating your own.

1) Perquisites:

- Microsoft Visual Studio: http://msdn.microsoft.com
- The Visual Studio SDK: http://msdn.microsoft.com/en-US/vstudio/vextend
- Telerik JustCode: http://www.telerik.com/products/justcode.aspx

2) How to use this project:

Get the sample source code and open JustCodeExtensions.sln inside Visual Studio. Once you Run/Debug another Visual Studio instance will start with the sample extensions loaded by JustCode. You can test the extensions inside the started Visual Studio instance. Currently, the samples include:
- JustCode.Analyzers: Custom analyzers that make JustCode show additional warnings
- JustCode.Cleaning: Custom code cleaning steps that are available as part of JustCode’s “Clean Code” feature
- JustCode.Navigation: Custom navigation features, that are available in JustCode’s “Navigate” Visual Aid section

You can add additional commands, analyzers or code cleaning steps inside the existing Github project. 

3) To create your own project:

To create your own JustCode extension project use the Visual Studio template that is installed as part of JustCode.

4) Distribution of your own project:

JustCode extensions can be packaged and distributed as Visual Studio Extensions in the Visual Studio Gallery. Search for “JustCode” in the Gallery and you’ll already find some of the samples of this Github project.
 

