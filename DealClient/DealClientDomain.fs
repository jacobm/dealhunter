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

