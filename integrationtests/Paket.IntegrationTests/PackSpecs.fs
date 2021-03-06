﻿module Paket.IntegrationTests.PackSpecs

open Fake
open System
open NUnit.Framework
open FsUnit
open System.IO
open System.IO.Compression
open Paket.Domain
open Paket
open Paket.NuGetCache
open Paket.Requirements

let getDependencies(x:Paket.NuGet.NuGetPackageCache) = x.GetDependencies()

[<Test>]
let ``#1234 empty assembly name``() = 
    let outPath = Path.Combine(scenarioTempPath "i001234-missing-assemblyname","out")
    try
        paket ("pack output \"" + outPath + "\"") "i001234-missing-assemblyname" |> ignore
        failwith "Expected an exeption"
    with
    | exn when exn.Message.Contains("PaketBug.dll") -> ()

    File.Delete(Path.Combine(scenarioTempPath "i001234-missing-assemblyname","PaketBug","paket.template"))

[<Test>]
let ``#1348 npm type folder names`` () =
    let rootPath = scenarioTempPath "i001348-packaging-npm-type-folders"
    let outPath = Path.Combine(rootPath,"out")
    let package = Path.Combine(outPath, "Paket.Integrations.Npm.1.0.0.nupkg")
    
    paket ("pack output \"" + outPath + "\"") "i001348-packaging-npm-type-folders" |> ignore 
    ZipFile.ExtractToDirectory(package, outPath)

    let desiredFolderName = "font-awesome@4.5.0"
    
    let extractedFolder = 
        Path.Combine(outPath,"jspm_packages", "npm") 
        |> directoryInfo 
        |> subDirectories 
        |> Array.head
    
    extractedFolder.Name |> shouldEqual desiredFolderName

    CleanDir rootPath

[<Test>]
let ``#1375 pack specific dependency``() = 
    let outPath = Path.Combine(scenarioTempPath "i001375-pack-specific","out")
    paket ("pack output \"" + outPath + "\"") "i001375-pack-specific" |> ignore

    File.Delete(Path.Combine(scenarioTempPath "i001375-pack-specific","PaketBug","paket.template"))

[<Test>]
let ``#1375 pack with projectUrl commandline``() = 
    let outPath = Path.Combine(scenarioTempPath "i001375-pack-specific","out")
    paket ("pack output \"" + outPath + "\" project-url \"http://localhost\"") "i001375-pack-specific" |> ignore

    File.Delete(Path.Combine(scenarioTempPath "i001375-pack-specific","PaketBug","paket.template"))

[<Test>]
let ``#1376 fail template``() = 
    let outPath = Path.Combine(scenarioTempPath "i001376-pack-template","out")
    let templatePath = Path.Combine(scenarioTempPath "i001376-pack-template","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\" templatefile " + templatePath) "i001376-pack-template" |> ignore
    let fileInfo = FileInfo(Path.Combine(outPath, "PaketBug.1.0.0.0.nupkg"))
    let (expectedFileSize: int64) = int64(1542)
    fileInfo.Length |> shouldBeGreaterThan expectedFileSize

    File.Delete(Path.Combine(scenarioTempPath "i001376-pack-template","PaketBug","paket.template"))

[<Test>]
let ``#1376 template with plus``() = 
    let outPath = Path.Combine(scenarioTempPath "i001376-pack-template-plus","out")
    let templatePath = Path.Combine(scenarioTempPath "i001376-pack-template-plus","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\" templatefile " + templatePath) "i001376-pack-template-plus" |> ignore
    let fileInfo = FileInfo(Path.Combine(outPath, "PaketBug.1.0.0.0.nupkg"))
    let (expectedFileSize: int64) = int64(1542)
    fileInfo.Length |> shouldBeGreaterThan expectedFileSize
 
    ZipFile.ExtractToDirectory(fileInfo.FullName, outPath)

    let expectedFile = Path.Combine(outPath, "content", "net45+net451", "paket.references")

    File.Exists expectedFile |> shouldEqual true
    File.Delete(templatePath)

    File.Delete(Path.Combine(scenarioTempPath "i001376-pack-template-plus","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps from template``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\" templatefile " + templatePath) "i001429-pack-deps" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldNotContain (PackageName "PaketBug2") // it's not packed in same round
    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldNotContain (PackageName "PaketBug")

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\"") "i001429-pack-deps" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "PaketBug2")
    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldNotContain (PackageName "PaketBug")

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps using minimum-from-lock-file``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","PaketBug", "paket.template")
    paket ("pack minimum-from-lock-file output \"" + outPath + "\"") "i001429-pack-deps-minimum-from-lock" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    let packageName, versionRequirement, restrictions = details |> getDependencies |> Seq.filter (fun (x,_,_) -> x = PackageName "MySql.Data") |> Seq.head 
    versionRequirement |> shouldNotEqual (VersionRequirement.AllReleases)

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps without minimum-from-lock-file uses dependencies file range``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\"") "i001429-pack-deps-minimum-from-lock" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    let packageName, versionRequirement, restrictions = details |> getDependencies |> Seq.filter (fun (x,_,_) -> x = PackageName "MySql.Data") |> Seq.head 
    versionRequirement |> shouldEqual (VersionRequirement.Parse "1.2.3")

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps-minimum-from-lock","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps without minimum-from-lock-file uses specifc dependencies file range``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps-specific","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps-specific","PaketBug", "paket.template")
    paket ("pack output \"" + outPath + "\"") "i001429-pack-deps-specific" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    let packageName, versionRequirement, restrictions = details |> getDependencies |> Seq.filter (fun (x,_,_) -> x = PackageName "MySql.Data") |> Seq.head 
    versionRequirement |> shouldEqual (VersionRequirement.Parse "[2.3.4]")

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps-specific","PaketBug","paket.template"))

[<Test>]
let ``#1429 pack deps with minimum-from-lock-file uses specifc dependencies file range``() = 
    let outPath = Path.Combine(scenarioTempPath "i001429-pack-deps-specific","out")
    let templatePath = Path.Combine(scenarioTempPath "i001429-pack-deps-specific","PaketBug", "paket.template")
    paket ("pack minimum-from-lock-file  output \"" + outPath + "\"") "i001429-pack-deps-specific" |> ignore

    let details = 
        NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "PaketBug") (SemVer.Parse "1.0.0.0")
        |> Async.RunSynchronously
        |> ODataSearchResult.get

    details |> getDependencies |> Seq.map (fun (x,_,_) -> x) |> shouldContain (PackageName "MySql.Data")
    let packageName, versionRequirement, restrictions = details |> getDependencies |> Seq.filter (fun (x,_,_) -> x = PackageName "MySql.Data") |> Seq.head 
    versionRequirement |> shouldEqual (VersionRequirement.Parse "[2.3.4]")

    File.Delete(Path.Combine(scenarioTempPath "i001429-pack-deps-specific","PaketBug","paket.template"))

[<Test>]
let ``#1473 works in same folder``() =
    let scenario = "i001473-blocking"

    prepare scenario
    directPaket "pack templatefile paket.template output o" scenario |> ignore
    directPaket "update" scenario|> ignore

[<Test>]
let ``#1472 globs correctly``() =
    let scenario = "i001472-globbing"

    let outPath = Path.Combine(scenarioTempPath scenario,"out")
    let templatePath = Path.Combine(scenarioTempPath scenario,"src", "A.Source", "paket.template")
    paket ("pack version 1.0.0 output \"" + outPath + "\" -v") scenario |> ignore

    let package = Path.Combine(outPath, "A.Source.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)

    let expectedFile = Path.Combine(outPath, "content", "A", "Folder", "source.cs")

    File.Exists expectedFile |> shouldEqual true

[<Test>]
let ``#1472 allows to put stuff in root of package``() =
    let scenario = "i001472-pack-in-root"

    let outPath = Path.Combine(scenarioTempPath scenario,"out")
    let templatePath = Path.Combine(scenarioTempPath scenario,"src", "A.Source", "paket.template")
    paket ("pack version 1.0.0 output \"" + outPath + "\" -v") scenario |> ignore

    let package = Path.Combine(outPath, "A.Source.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)

    let expectedFile = Path.Combine(outPath, "Folder", "source.cs")

    File.Exists expectedFile |> shouldEqual true
    File.Delete(templatePath)

[<Test>]
let ``#1472 allows to put stuff in relative folder``() =
    let scenario = "i001472-pack-in-relative"

    let outPath = Path.Combine(scenarioTempPath scenario,"out")
    let templatePath = Path.Combine(scenarioTempPath scenario,"src", "A.Source", "paket.template")
    paket ("pack version 1.0.0 output \"" + outPath + "\" -v") scenario |> ignore

    let package = Path.Combine(outPath, "A.Source.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)

    let expectedFile = Path.Combine(outPath, "A", "Folder", "source.cs")

    File.Exists expectedFile |> shouldEqual true
    File.Delete(templatePath)

[<Test>]
let ``#1483 pack deps with locked version from group``() = 
    let outPath = Path.Combine(scenarioTempPath "i001483-group-lock","out")
    let templatePath = Path.Combine(scenarioTempPath "i001483-group-lock","pack", "paket.template")
    paket ("pack output \"" + outPath + "\"") "i001483-group-lock" |> ignore

    File.Delete(templatePath)

[<Test>]
let ``#1506 allows to pack files without ending``() =
    let scenario = "i001506-pack-ending"

    let outPath = Path.Combine(scenarioTempPath scenario,"out")
    let templatePath = Path.Combine(scenarioTempPath scenario, "paket.template")
    paket ("pack output \"" + outPath + "\" -v") scenario |> ignore

    let package = Path.Combine(outPath, "Foo.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)
    
    File.Exists(Path.Combine(outPath, "tools", "blah.foo")) |> shouldEqual true
    File.Exists(Path.Combine(outPath, "tools", "blah")) |> shouldEqual true
    File.Delete(templatePath)

[<Test>]
let ``#1514 invalid pack should give proper warning``() =
    let scenario = "i001514-pack-error"

    let outPath = Path.Combine(scenarioTempPath scenario,"out")
    let templatePath = Path.Combine(scenarioTempPath scenario, "paket.template")

    try
        paket ("pack buildconfig \"Debug\" buildplatform \"AnyCPU\" output \"" + outPath + "\" -v") scenario |> ignore
        failwith ""
    with
    | exn when exn.Message.Contains "No package with id 'PaketDemo.MyLibrary'" -> ()

    File.Delete(templatePath)

[<Test>]
let ``#1538 symbols src folder structure`` () =
    let scenario = "i001538-symbols-src-folder-structure"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "PackWithSource.1.0.0.0.symbols.nupkg")
    
    paket ("pack output \"" + outPath + "\" symbols") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "net452", "PackWithSource.pdb") |> checkFileExists

    let srcRoot = Path.Combine(outPath, "src", "PackWithSource")
    Path.Combine(srcRoot, "ClassInSolutionRoot.cs") |> checkFileExists
    Path.Combine(srcRoot, "LinkedInSolutionRoot.cs") |> checkFileExists
    Path.Combine(srcRoot, "Folder", "ClassInFolder.cs") |> checkFileExists
    Path.Combine(srcRoot, "Folder", "LinkedInFolder.cs") |> checkFileExists
    Path.Combine(srcRoot, "Folder", "NestedFolder", "ClassInNestedFolder.cs") |> checkFileExists
    Path.Combine(srcRoot, "Folder", "NestedFolder", "LinkedInNestedFolder.cs") |> checkFileExists
    Path.Combine(srcRoot, "Properties", "AssemblyInfo.cs") |> checkFileExists

    CleanDir rootPath

[<Test>]
[<Ignore("ignore until we hear back")>]
let ``#1504 unpacking should override``() =
    let scenario = "i001504-override"

    prepare scenario
    directPaket "pack templatefile paket.B.template version 1.0.0 output bin" scenario |> ignore
    directPaket "pack templatefile paket.A.template version 1.0.0 output bin" scenario |> ignore
    directPaket "update" scenario|> ignore

[<Test>]
let ``#1586 pack dependent projects``() =
    let scenario = "i001586-pack-referenced"

    prepare scenario
    directPaket "pack output . include-referenced-projects minimum-from-lock-file -v" scenario |> ignore

[<Test>]
let ``#1594 allows to pack directly``() =
    let scenario = "i001594-pack"

    let outPath = Path.Combine(scenarioTempPath scenario,"bin")
    let templatePath = Path.Combine(scenarioTempPath scenario, "paket.template")
    paket "pack output bin version 1.0.0 templatefile paket.template" scenario |> ignore

    let package = Path.Combine(outPath, "ClassLibrary1.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)
    
    File.Exists(Path.Combine(outPath, "lib", "net35", "ClassLibrary1.dll")) |> shouldEqual true
    File.Delete(templatePath)

[<Test>]
let ``#1596 pack works for reflected definition assemblies``() =
    let scenario = "i001596-pack-reflectedDefinition"

    let outPath = Path.Combine(scenarioTempPath scenario,"bin")
    let templatePath = Path.Combine(scenarioTempPath scenario, "paket.template")
    let r = paket "pack output bin version 1.0.0 templatefile paket.template" scenario 
    printfn "paket.pack said: %A" r
    let package = Path.Combine(outPath, "Project2.1.0.0.nupkg")
 
    ZipFile.ExtractToDirectory(package, outPath)
    
    File.Exists(Path.Combine(outPath, "lib", "net45", "Project2.dll")) |> shouldEqual true
    File.Delete(templatePath)

[<Test>]
let ``#1816 pack localized happy path`` () =
    let scenario = "i001816-pack-localized-happy-path"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "LocalizedLib.1.0.0.0.nupkg")
    
    paket ("pack -v output \"" + outPath + "\"") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "net45", "LocalizedLib.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "net45", "sv", "LocalizedLib.resources.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "net45", "sv-FI", "LocalizedLib.resources.dll") |> checkFileExists

    CleanDir rootPath

[<Test>]
let ``#1816 pack localized when satellite dll is missing`` () =
    let scenario = "i001816-pack-localized-missing-dll"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "LocalizedLib.1.0.0.0.nupkg")
    
    let result = paket ("pack -v output \"" + outPath + "\"") scenario
    let expectedMessage = "Did not find satellite assembly for (sv) try building and running pack again."
    StringAssert.Contains(expectedMessage, result)
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "net45", "LocalizedLib.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "net45", "sv-FI", "LocalizedLib.resources.dll") |> checkFileExists

    CleanDir rootPath

[<Test>]
let ``#3275 netstandard pack localized happy path`` () =
    let scenario = "i003275-pack-localized-netstandard"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "LibForTest.1.0.0.nupkg")

    paket ("pack -v output \"" + outPath + "\"") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "netstandard2.0", "LibForTest.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "de", "LibForTest.resources.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "ru", "LibForTest.resources.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "en-US", "LibForTest.resources.dll") |> checkFileExists

    CleanDir rootPath

[<Test>]
let ``#1848 single template without include-referenced-projects`` () = 
    let scenario = "i001848-pack-single-template-wo-incl-flag"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let templatePath = Path.Combine(rootPath, "projectA", "paket.template")
    paket ("pack --template " + templatePath + " \"" + outPath + "\"") scenario |> ignore

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectA") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> shouldBeEmpty

    ZipFile.ExtractToDirectory(Path.Combine(outPath, "projectA.1.0.0.0.nupkg"), outPath)
    Path.Combine(outPath, "lib", "net45", "projectB.dll") |> checkFileExists

    CleanDir rootPath

[<Test>]
let ``#1848 single template with include-referenced-projects`` () = 
    let scenario = "i001848-pack-single-template-with-incl-flag"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let templatePath = Path.Combine(rootPath, "projectA", "paket.template")
    paket ("pack --include-referenced-projects --template  " + templatePath + " \"" + outPath + "\"") scenario |> ignore

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectA") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "projectB" && version = VersionRequirement.Parse "1.0.0.0") 
    |> shouldNotEqual None

    ZipFile.ExtractToDirectory(Path.Combine(outPath, "projectA.1.0.0.0.nupkg"), outPath)
    let expectedFile = Path.Combine(outPath, "lib", "net45", "projectB.dll")

    File.Exists expectedFile |> shouldEqual false

    CleanDir rootPath

[<Test>]
let ``#1848 all templates without include-referenced-projects`` () = 
    let scenario = "i001848-pack-all-templates-wo-incl-flag"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    paket ("pack \"" + outPath + "\"") scenario |> ignore

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectA") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "projectB" && version = VersionRequirement.Parse "1.0.0.0") 
    |> shouldNotEqual None

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectB") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "nunit" && version = VersionRequirement.Parse "[3.8.1]") 
    |> shouldNotEqual None

    ZipFile.ExtractToDirectory(Path.Combine(outPath, "projectA.1.0.0.0.nupkg"), outPath)
    let expectedFile = Path.Combine(outPath, "lib", "net45", "projectB.dll")
    File.Exists expectedFile |> shouldEqual false

    CleanDir rootPath

[<Test>]
let ``#1848 all templates with include-referenced-projects`` () = 
    let scenario = "i001848-pack-all-templates-with-incl-flag"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    paket ("pack --include-referenced-projects \"" + outPath + "\"") scenario |> ignore

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectA") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "projectB" && version = VersionRequirement.Parse "1.0.0.0") 
    |> shouldNotEqual None

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectB") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "nunit" && version = VersionRequirement.Parse "[3.8.1]") 
    |> shouldNotEqual None

    ZipFile.ExtractToDirectory(Path.Combine(outPath, "projectA.1.0.0.0.nupkg"), outPath)
    let expectedFile = Path.Combine(outPath, "lib", "net45", "projectB.dll")
    File.Exists expectedFile |> shouldEqual false

    CleanDir rootPath

[<Test>]
let ``#1848 include-referenced-projects with non-packed project dependencies`` () = 
    let scenario = "i001848-pack-with-non-packed-deps"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    paket ("pack --include-referenced-projects \"" + outPath + "\"") scenario |> ignore

    NuGetLocal.getDetailsFromLocalNuGetPackage false None outPath "" (PackageName "projectA") (SemVer.Parse "1.0.0.0")
    |> Async.RunSynchronously
    |> ODataSearchResult.get
    |> getDependencies 
    |> Seq.tryFind (fun (name,version,_) -> name = PackageName "nunit" && version = VersionRequirement.Parse "[3.8.1]") 
    |> shouldNotEqual None    
    
    ZipFile.ExtractToDirectory(Path.Combine(outPath, "projectA.1.0.0.0.nupkg"), outPath)
    Path.Combine(outPath, "lib", "net45", "projectB.dll") |> checkFileExists
    
    CleanDir rootPath

[<Test>]
let ``#2694 paket fixnuspec should not remove project references``() = 
    let project = "console"
    let scenario = "i002694"
    prepareSdk scenario

    let wd = (scenarioTempPath scenario) @@ project

    directDotnet true (sprintf "pack %s.csproj" project) wd
        |> ignore

    let nupkgPath = wd @@ "bin" @@ "Debug" @@ project + ".1.0.0.nupkg"
    if File.Exists nupkgPath |> not then Assert.Fail(sprintf "Expected '%s' to exist" nupkgPath)
    let nuspec = NuGetLocal.getNuSpecFromNupgk nupkgPath
    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "library") with
    | None -> Assert.Fail("Expected package to still contain the project reference!")
    | Some s -> ignore s
    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "FSharp.Core") with
    | None -> Assert.Fail("Expected package to still contain the FSharp.Core reference!")
    | Some s -> ignore s

    // Should we remove Microsoft.NETCore.App?
    // Problably not as "packaged" console applications have this dependency by default, see https://www.nuget.org/packages/dotnet-mergenupkg
    nuspec.Dependencies.Value.Length
    |> shouldEqual 3
    
[<Test>]
let ``#2765 pack single template does not evaluate other template`` () = 
    let scenario = "i002765-evaluate-only-single-template"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let templatePath = Path.Combine(rootPath, "ProjectA", "paket.template")
    Assert.DoesNotThrow(fun () -> paket ("pack --template " + templatePath + " \"" + outPath + "\"") scenario |> ignore)
    CleanDir rootPath    

[<Test>]
let ``#2788 with include-pdbs true`` () = 
    let scenario = "i002788-pack-with-include-pdbs-true"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "BuiltWithSymbols.1.0.0.0.nupkg")
    paket ("pack \"" + outPath + "\"") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "net45", "BuiltWithSymbols.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "net45", "BuiltWithSymbols.xml") |> checkFileExists
    Path.Combine(outPath, "lib", "net45", "BuiltWithSymbols.pdb") |> checkFileExists

    CleanDir rootPath
    
[<Test>]
let ``#3164 pack analyzer`` () = 
    let scenario = "i003164-pack-analyzer"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    paket ("pack \"" + outPath + "\"") scenario |> ignore

    let package = Path.Combine(outPath, "Analyzer.0.2.0.3-dev.nupkg")
    ZipFile.ExtractToDirectory(package, outPath)
    Path.Combine(outPath, "analyzers", "dotnet", "cs", "Analyzer.dll") |> checkFileExists

    CleanDir rootPath    

    
[<Test>]
let ``#3165 pack multitarget with p2p`` () = 
    let scenario = "i003165-pack-multitarget-with-p2p"
    prepareSdk scenario
    let rootPath = scenarioTempPath scenario

    directDotnet true "build MyProj.Main -c Release" rootPath
    |> Seq.iter (printfn "%A")

    let outPath = Path.Combine(rootPath, "out")
    directPaket (sprintf """pack "%s" """ outPath) scenario
    |> Seq.iter (printfn "%A")

    let nupkgPath = Path.Combine(outPath, "MyProj.Main.1.0.0.nupkg")

    if File.Exists nupkgPath |> not then Assert.Fail(sprintf "Expected '%s' to exist" nupkgPath)
    let nuspec = NuGetLocal.getNuSpecFromNupgk nupkgPath
    let depsByTfm byTfm = nuspec.Dependencies.Value |> Seq.choose (fun (pkgName,version,tfm) -> if (tfm.GetExplicitRestriction()) = byTfm then Some (pkgName,version) else None) |> Seq.toList
    let pkgVer name version = (PackageName name), (VersionRequirement.Parse version)

    let tfmNET45 = FrameworkIdentifier.DotNetFramework(FrameworkVersion.V4_5)
    CollectionAssert.AreEquivalent([ pkgVer "FSharp.Core" "3.1.2.5"; pkgVer "Argu" "4.2.1" ], depsByTfm (FrameworkRestriction.AtLeast(tfmNET45)))

    let tfmNETSTANDARD2_0 = FrameworkIdentifier.DotNetStandard(DotNetStandardVersion.V2_0)
    CollectionAssert.AreEquivalent([ pkgVer "FSharp.Core" "4.5.1"; pkgVer "Argu" "5.1.0" ], depsByTfm (FrameworkRestriction.And [FrameworkRestriction.NotAtLeast(tfmNET45); FrameworkRestriction.AtLeast(tfmNETSTANDARD2_0)]))

    CollectionAssert.AreEquivalent([ pkgVer "MyProj.Common" "1.0.0" ], depsByTfm (FrameworkRestriction.Or [FrameworkRestriction.AtLeast(tfmNET45); FrameworkRestriction.AtLeast(tfmNETSTANDARD2_0)]))

    let unzippedNupkgPath = Path.Combine(outPath, "MyProj.Main")
    ZipFile.ExtractToDirectory(nupkgPath, unzippedNupkgPath)
    Path.Combine(unzippedNupkgPath, "lib", "net45", "MyProj.Main.dll") |> checkFileExists
    Path.Combine(unzippedNupkgPath, "lib", "netstandard2.0", "MyProj.Main.dll") |> checkFileExists

    CleanDir rootPath    

[<Test>]
let ``#4002 dotnet pack of a global tool shouldnt contain references``() = 
    let project = "tool1"
    let scenario = "i004002-pack-global-tools"
    prepareSdk scenario

    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")

    directPaket ("restore") scenario
    |> ignore

    directDotnet true (sprintf "pack -o \"%s\" /p:PackAsTool=true /bl" outPath) rootPath
    |> ignore

    let nupkgPath = Path.Combine(outPath, project + ".1.0.0.nupkg")
    if File.Exists nupkgPath |> not then Assert.Fail(sprintf "Expected '%s' to exist" nupkgPath)
    let nuspec = NuGetLocal.getNuSpecFromNupgk nupkgPath

    printfn "%A" nuspec

    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "FSharp.Core") with
    | Some s -> Assert.Fail(sprintf "Expected package to still contain the FSharp.Core reference! %A" s)
    | None -> ()

    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "Argu") with
    | Some s -> Assert.Fail(sprintf "Expected package to still contain the Argu reference! %A" s)
    | None -> ()

    // Should we remove Microsoft.NETCore.App?
    // Problably not as "packaged" console applications have this dependency by default, see https://www.nuget.org/packages/dotnet-mergenupkg
    nuspec.Dependencies.Value.Length
    |> shouldEqual 0

    
[<Test>]
let ``#4003 dotnet pack of a global tool with p2p``() = 
    let project = "tool1"
    let scenario = "i004003-pack-global-tools-p2p"
    prepareSdk scenario

    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")

    directPaket ("restore") scenario
    |> ignore

    directDotnet true (sprintf "pack tool1 -o \"%s\" /bl" outPath) rootPath
    |> ignore

    let nupkgPath = Path.Combine(outPath, project + ".1.0.0.nupkg")
    if File.Exists nupkgPath |> not then Assert.Fail(sprintf "Expected '%s' to exist" nupkgPath)
    let nuspec = NuGetLocal.getNuSpecFromNupgk nupkgPath

    printfn "%A" nuspec

    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "FSharp.Core") with
    | Some s -> Assert.Fail(sprintf "Expected package to still contain the FSharp.Core reference! %A" s)
    | None -> ()

    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "Argu") with
    | Some s -> Assert.Fail(sprintf "Expected package to still contain the Argu reference! %A" s)
    | None -> ()

    match nuspec.Dependencies.Value |> Seq.tryFind (fun (name,_,_) -> name = PackageName "Suave") with
    | Some s -> Assert.Fail(sprintf "Expected package to still contain the Suave reference! %A" s)
    | None -> ()

    // Should we remove Microsoft.NETCore.App?
    // Problably not as "packaged" console applications have this dependency by default, see https://www.nuget.org/packages/dotnet-mergenupkg
    nuspec.Dependencies.Value.Length
    |> shouldEqual 0

[<Test>]
let ``#4010-pack-template-only``() =
    let scenario = "i004010-pack-template-only"
    let outPath = Path.Combine(scenarioTempPath scenario, "out")
    let templatePath = Path.Combine(scenarioTempPath scenario, "PaketBug", "paket.template")
    paket (sprintf """pack --template "%s" "%s" --version 1.2.3 """ templatePath outPath) scenario |> ignore

[<Test>]
let ``#2776 transitive project references included`` () =
    let scenario = "i002776-pack-transitive-project-refs"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "A.1.0.0.nupkg")

    paket ("pack --include-referenced-projects \"" + outPath + "\"") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "netstandard2.0", "A.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "B.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "C.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "D.dll") |> checkFileExists

    let nuspec = NuGetLocal.getNuSpecFromNupgk package
    let dependencies = nuspec.Dependencies.Value |> Seq.map (fun (x,_,_) -> x)
    dependencies |> shouldContain (PackageName "nlog")

    CleanDir rootPath

[<Test>]
let ``#2776 transitive references stops on project with template`` () =
    let scenario = "i002776-pack-transitive-with-template"
    let rootPath = scenarioTempPath scenario
    let outPath = Path.Combine(rootPath, "out")
    let package = Path.Combine(outPath, "A.1.0.0.nupkg")

    paket ("pack --include-referenced-projects \"" + outPath + "\"") scenario |> ignore
    ZipFile.ExtractToDirectory(package, outPath)

    Path.Combine(outPath, "lib", "netstandard2.0", "A.dll") |> checkFileExists
    Path.Combine(outPath, "lib", "netstandard2.0", "B.dll") |> checkFileExists
    File.Exists(Path.Combine(outPath, "lib", "netstandard2.0", "C.dll")) |> shouldEqual false
    File.Exists(Path.Combine(outPath, "lib", "netstandard2.0", "D.dll")) |> shouldEqual false

    let nuspec = NuGetLocal.getNuSpecFromNupgk package
    let dependencies = nuspec.Dependencies.Value |> Seq.map (fun (x,_,_) -> x)

    dependencies |> shouldContain (PackageName "C")
    dependencies |> shouldNotContain (PackageName "nlog")

    CleanDir rootPath