using System;
using System.IO;
using System.Text.RegularExpressions;

var proj = @"d:\EXE\RehabTracking\RehabTracking.Web\RehabTracking.Web.csproj";
var text = File.ReadAllText(proj);

if (!text.Contains("<EmbeddedResource Update=")) {
    var insert = @"
    <ItemGroup>
      <Compile Update=""Resources\ErrorMessages.Designer.cs"">
        <DesignTime>True</DesignTime>
        <AutoGen>True</AutoGen>
        <DependentUpon>ErrorMessages.resx</DependentUpon>
      </Compile>
      <Compile Update=""Resources\Messages.Designer.cs"">
        <DesignTime>True</DesignTime>
        <AutoGen>True</AutoGen>
        <DependentUpon>Messages.resx</DependentUpon>
      </Compile>
      <EmbeddedResource Update=""Resources\ErrorMessages.resx"">
        <Generator>PublicResXFileCodeGenerator</Generator>
        <LastGenOutput>ErrorMessages.Designer.cs</LastGenOutput>
      </EmbeddedResource>
      <EmbeddedResource Update=""Resources\Messages.resx"">
        <Generator>PublicResXFileCodeGenerator</Generator>
        <LastGenOutput>Messages.Designer.cs</LastGenOutput>
      </EmbeddedResource>
    </ItemGroup>
</Project>";
    text = text.Replace("</Project>", insert);
    File.WriteAllText(proj, text);
    Console.WriteLine("Updated csproj");
}
