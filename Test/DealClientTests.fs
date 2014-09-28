namespace DealClientTests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

module FsGenerators =
    open DealClient.DealClientDomain
    // http://fscheck.codeplex.com/wikipage?title=Test%20Data%20Generators
    let eventGen() : Gen<UserEvent> =
        Gen.oneof [ gen { return Watch "fisk"}; gen { return Unwatch "fisk"}]
    type Generators =
        static member UserEvent() = eventGen() |> Arb.fromGen 

module DealClientDomainTests =
    open DealClient.DealClientDomain

    [<Fact>]
    let ``empty seq is not watched`` () =
        Assert.True(getPosition Seq.empty = (false, None))

    [<Fact>]
    let ``seq with only Watch is watched`` () =
        let events = [Watch "dingo"]
        Assert.True(getPosition events = (true, None))

    [<Fact>]
    let ``Unwatch results in term not being watched`` () =
        let events = [Watch "dingo"; Unwatch "dingo"]
        Assert.True(getPosition events = (false, None))

    [<Fact>]
    let ``SetTermPosition sets position`` () =
        let events = [Watch "dingo"; SetTermPosition {term = "dingo"; position = "1"}]
        Assert.True(getPosition events = (true, Some "1"))

    [<Property>]
    let ``square should be positive`` (x:float) =
        x * x > 0.

    [<Property>]
    let ``Testings`` (x:UserEvent list) =
        true