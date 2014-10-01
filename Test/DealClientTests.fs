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

    let generateSetPosition term : Gen<UserEvent> =
        gen {
            let! pos = Arb.generate<string>
            return SetTermPosition (term, pos)
        }

    let openListGenerator()  : Gen<UserEvent list> =
        gen {
            let start = Watch "dingo"
            let! pos = Gen.listOf (generateSetPosition "dingo")
            let startHest = Watch "hest"

            let! concat = [(generateSetPosition "dingo"); (generateSetPosition "hest")] |> Gen.sequence

            return [start] @ pos @ [startHest] @ concat
        }

    type MyGenerators =
        static member UserEvent() = eventGen() |> Arb.fromGen 
        static member OpenListGenerator() = openListGenerator() |> Arb.fromGen

module DealClientDomainTests =
    open FsGenerators
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
        let state = (getPosition events).Value
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

    [<Fact>]
    let ``getTermPosition returns watched term's postition`` () =
        let state = {term = "dingo"; isWatched = true; position = (Some "pos") }
        let result = getTermState "dingo" [state]
        Assert.True(result.Value.position = (Some "pos"))

    [<Fact>]
    let ``getTermPosition returns None for not watched term`` () =
        let state = {term = "dingo"; isWatched = true; position = (Some "pos") }
        let result = getTermState "fisk" [state]
        Assert.True(result.IsNone)

    [<Property(Arbitrary=[|typeof<MyGenerators>|])>]
    let ``Two open sequences`` (events : UserEvent list) =
        let isPosition term event =
            match event with
            | SetTermPosition (t, _) when t = term -> true
            | _ -> false

        let getPosition event =
            match event with
            | SetTermPosition (_, pos) -> pos
            | _ -> null

        let result = buildState events

        let test (state : Position) =
            Assert.True(state.isWatched)
            let pos = events |> List.rev |> List.find (isPosition state.term) |> getPosition
            let result = pos = state.position.Value
            Assert.True(result)

        Assert.True(Seq.length result = 2)
        result |> Seq.iter test
