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
        Assert.True(getPosition Seq.empty = None)

    [<Fact>]
    let ``seq with only Watch is watched`` () =
        let events = [Watch "dingo"]
        Assert.True(Option.isSome (getPosition events))

    [<Fact>]
    let ``Unwatch results in term not being watched`` () =
        let events = [Watch "dingo"; Unwatch "dingo"]
        Assert.True(Option.isSome (getPosition events))

    [<Fact>]
    let ``SetTermPosition sets position`` () =
        let events = [Watch "dingo"; SetTermPosition ("dingo", "1")]
        let (Some state) = getPosition events
        Assert.True(state.isWatched)
        Assert.True(state.term = "dingo")
        Assert.True(state.position = Some "1")

    [<Fact>]
    let ``buildState return watch on dingo and hest`` () =
        let events = [Watch "dingo"; SetTermPosition ("dingo", "1"); Watch "hest"]
        let result = buildState events
        Assert.True(Seq.length result = 2)
        let dingo = Seq.find (fun x -> x.term = "dingo") result
        Assert.True(dingo.isWatched)
        Assert.True(dingo.position = Some "1")
        let hest = Seq.find (fun x -> x.term = "hest") result
        Assert.True(hest.isWatched)
        Assert.True(hest.position = None)

    [<Property>]
    let ``Testings`` (x:UserEvent list) =
        true