namespace DealClientTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit
open DealClient.DealClientDomain

module FsGenerators =

    // http://fscheck.codeplex.com/wikipage?title=Test%20Data%20Generators
    let eventGen() : Gen<UserEvent> =
        Gen.oneof [ gen { return Watch "fisk"}; gen { return Unwatch "fisk"}]
    type Generators =
        static member UserEvent() = eventGen() |> Arb.fromGen 
        

module DealClientDomainTests =

    [<Fact>]
    let ``length above 8 should be valid`` () =
        Assert.True(true)

    [<Fact>]
    let ``another test`` () =
        Assert.True(true)

    [<Property>]
    let ``square should be positive`` (x:float) =
        x * x > 0.

    [<Property>]
    let ``Testings`` (x:UserEvent list) =
        true