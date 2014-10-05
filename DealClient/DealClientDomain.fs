namespace DealClient

module DealClientDomain =
    type Term = string
    type TermPosition = { term : Term; position : string }
    type UserData = { positions : TermPosition list }
    type GoogleId = Id of string
    type UserEvent =
    | Watch of Term
    | SetTermPosition of Term * string
    | Unwatch of Term
    type Position = {term: Term; isWatched: bool; position: string option}

    let getPosition events =
        let accfunc acc event =
            match (acc, event) with
            | (_, Watch t) -> Some {term = t; isWatched = true; position = None }
            | (_, Unwatch t) -> Some {term = t; isWatched = false; position = None }
            | (Some _, SetTermPosition (t, pos)) -> Some {term = t; isWatched = true; position = Some pos}
            | (None, SetTermPosition (t, _)) -> Some {term = t; isWatched = false; position = None}
        Seq.fold accfunc None events

    let getTerm event =
        match event with
        | Watch t -> t
        | SetTermPosition (t, _) -> t
        | Unwatch t -> t

    let buildState events =
        let state = Seq.groupBy getTerm events
                    |> Seq.map (fun x -> snd x)
                    |> Seq.map getPosition
                    |> Seq.filter (fun x -> Option.isSome x)
                    |> Seq.map (fun x -> x.Value)
        state

    let getTermState term state =
        let term = Seq.filter (fun x -> x.term = term) state
        if (Seq.isEmpty term) then None else Some (Seq.head term)

    type DomainOp =
    | Success
    | Failure of string

    let termState reader googleId term =
        (reader >> buildState >> (getTermState term)) googleId

    let startWatching reader writer googleId term =
        let termState = termState reader googleId term
        if termState.IsNone
            then writer googleId term (Watch term)
                 Success
            else
                 Failure "Already watching"

    let setTermPosition reader writer googleId term pos =
        let termState = termState reader googleId term
        match termState with
        | None -> Failure "Not watching term and cannot set position"
        | Some _ -> let event = SetTermPosition (term, pos)
                    writer googleId term event
                    Success

    let stopWatching reader writer googleId term =
        let termState = termState reader googleId term
        if termState.IsNone
            then writer googleId term (Unwatch term)
                 Failure "Not watching"
            else
                Success
