namespace Encodings
[<AutoOpen>]
module PauliOperatorRegisterSequence =
    open System
    open System.Numerics
    open System.Collections.Generic

    type PauliOperatorRegisterSequence =
        {
            Coefficient : Complex
            Terms       : PauliRegister []
        }
        with
            static member Apply terms = {
                Terms = terms
                Coefficient = Complex.One
            }

            override this.ToString() =
                let buildString result (term : PauliRegister) =
                    let termStr =
                        term.Operators
                        |> Array.map (sprintf "%A")
                        |> (fun rg -> System.String.Join ("", rg))

                    if String.IsNullOrWhiteSpace result then
                        sprintf "%s%s" (term.GlobalPhase.PhasePrefix) termStr
                    else
                        sprintf "%s%s%s" result (term.GlobalPhase.ConjoiningSignAndPhase) termStr
                this.Terms
                |> Array.fold buildString ""

            static member private BuildMap (dict : Dictionary<string, Complex>) (key, curr) =
                if (dict.ContainsKey key) then
                    dict.[key] <- (dict.[key] + curr)
                else
                    dict.[key] <- curr
                dict

            static member (*) (l : PauliOperatorRegisterSequence, r : PauliOperatorRegisterSequence) =
                [|
                    for lt in l.Terms do
                        for rt in r.Terms do
                            let result = (lt * rt)
                            yield (result.ToString(), result.GlobalPhase)
                |]
                |> Array.fold PauliOperatorRegisterSequence.BuildMap (new Dictionary<string, Complex> ())
                |> Seq.map (fun kvp -> PauliRegister(kvp.Key, kvp.Value))
                |> Seq.toArray
                |> PauliOperatorRegisterSequence.Apply
